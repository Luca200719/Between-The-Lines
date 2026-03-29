from __future__ import annotations

from dataclasses import dataclass
from typing import Dict, List, Tuple
import re
import numpy as np

from sentence_transformers import SentenceTransformer
from transformers import pipeline


@dataclass
class ConversationInput:
    messages: List[str]
    question: str
    answer: str

    def validate(self) -> None:
        if not isinstance(self.messages, list) or len(self.messages) != 4:
            raise ValueError("messages must be a list of exactly 4 strings.")
        if not all(isinstance(m, str) and m.strip() for m in self.messages):
            raise ValueError("Each message must be a non-empty string.")
        if not isinstance(self.question, str) or not self.question.strip():
            raise ValueError("question must be a non-empty string.")
        if not isinstance(self.answer, str) or not self.answer.strip():
            raise ValueError("answer must be a non-empty string.")


class ResponseScoringAI:
    def __init__(
        self,
        embedding_model: str = "sentence-transformers/all-MiniLM-L6-v2",
        zero_shot_model: str = "facebook/bart-large-mnli",
        emotion_model: str = "j-hartmann/emotion-english-distilroberta-base",
    ) -> None:
        self.embedder = SentenceTransformer(embedding_model)

        self.zero_shot = pipeline(
            "zero-shot-classification",
            model=zero_shot_model,
            device=-1,
        )

        self.emotion_classifier = pipeline(
            "text-classification",
            model=emotion_model,
            top_k=None,
            device=-1,
        )

    # ------------------------------------------------------------
    # Public API
    # ------------------------------------------------------------

    def analyze(self, sample: ConversationInput) -> Dict:
        sample.validate()

        question_focus = self.detect_question_focus(sample.question)

        emotion_accuracy, emotion_alignment = self._score_emotion_accuracy_with_alignment(
            messages=sample.messages,
            question=sample.question,
            answer=sample.answer,
            question_focus=question_focus,
        )

        empathy = self._score_empathy(
            messages=sample.messages,
            question=sample.question,
            answer=sample.answer,
            question_focus=question_focus,
        )

        blame = self._score_blame(
            messages=sample.messages,
            question=sample.question,
            answer=sample.answer,
            question_focus=question_focus,
        )

        certainty = self._score_certainty(
            messages=sample.messages,
            question=sample.question,
            answer=sample.answer,
            question_focus=question_focus,
        )

        intent_accuracy = self._score_intent_accuracy(
            messages=sample.messages,
            question=sample.question,
            answer=sample.answer,
            question_focus=question_focus,
            emotion_alignment=emotion_alignment,
            empathy_score=empathy,
            blame_score=blame,
        )

        return {
            "conversation": sample.messages,
            "question": sample.question,
            "answer": sample.answer,
            "question_focus": question_focus,
            "scores": {
                "intent_accuracy": intent_accuracy,
                "emotion_accuracy": emotion_accuracy,
                "empathy": empathy,
                "blame": blame,
                "certainty": certainty,
            },
        }

    # ------------------------------------------------------------
    # Focus detection
    # ------------------------------------------------------------

    def detect_question_focus(self, question: str) -> str:
        q = question.lower()

        # Fast explicit routing first
        emotion_hit = any(w in q for w in ["feeling", "feel", "emotion", "emotional", "mood", "stressed", "sad", "angry"])
        certainty_hit = any(w in q for w in ["certain", "certainty", "sure", "confident", "confidence", "unsure", "uncertain"])
        empathy_hit = any(w in q for w in ["empathy", "empathetic", "compassion", "supportive", "caring"])
        blame_hit = any(w in q for w in ["blame", "fault", "responsibility", "accuse", "judgment"])
        intent_hit = any(w in q for w in ["intent", "motive", "reason", "why", "goal", "plan", "trying to"])

        hits = sum([emotion_hit, certainty_hit, empathy_hit, blame_hit, intent_hit])

        if hits >= 2:
            return "general"
        if emotion_hit:
            return "emotion"
        if certainty_hit:
            return "certainty"
        if empathy_hit:
            return "empathy"
        if blame_hit:
            return "blame"
        if intent_hit:
            return "intent"

        # Fallback to zero-shot
        labels = ["intent", "emotion", "empathy", "blame", "certainty"]
        result = self.zero_shot(question, candidate_labels=labels, multi_label=True)
        scores = dict(zip(result["labels"], result["scores"]))
        ranked = sorted(scores.items(), key=lambda x: x[1], reverse=True)

        top_label, top_score = ranked[0]
        second_score = ranked[1][1] if len(ranked) > 1 else 0.0

        if top_score < 0.45 or second_score > 0.38:
            return "general"
        return top_label

    # ------------------------------------------------------------
    # Metric scorers
    # ------------------------------------------------------------

    def _score_intent_accuracy(
        self,
        messages: List[str],
        question: str,
        answer: str,
        question_focus: str,
        emotion_alignment: float,
        empathy_score: int,
        blame_score: int,
    ) -> int:
        conversation_text = "\n".join(messages)
        context_bundle = f"Conversation:\n{conversation_text}\n\nQuestion:\n{question}"

        sim_context = self._similarity(answer, conversation_text)
        sim_question = self._similarity(answer, question)
        sim_bundle = self._similarity(answer, context_bundle)

        minimizing = self._contains_any(
            answer,
            [
                "overreacting", "not a big deal", "just move on", "too sensitive",
                "dramatic", "making a big deal", "should get over it"
            ]
        )

        descriptive = self._contains_any(
            answer,
            [
                "stressed", "discouraged", "upset", "sad", "terrible",
                "not fully sure", "unsure", "uncertain"
            ]
        )

        raw = (
            0.34 * sim_context +
            0.24 * sim_question +
            0.24 * sim_bundle
        )

        if descriptive:
            raw += 0.08

        if question_focus in {"intent", "general"}:
            raw += 0.04

        if emotion_alignment < 0.35:
            raw -= 0.20
        elif emotion_alignment < 0.50:
            raw -= 0.10

        if blame_score >= 4:
            raw -= 0.14
        elif blame_score == 3:
            raw -= 0.08

        if empathy_score == 1:
            raw -= 0.08

        if minimizing:
            raw -= 0.15

        raw = max(0.0, min(1.0, raw))
        return self._to_1_5(raw)

    def _score_emotion_accuracy_with_alignment(
        self,
        messages: List[str],
        question: str,
        answer: str,
        question_focus: str,
    ) -> Tuple[int, float]:
        conversation_text = "\n".join(messages)

        convo_emotions = self._emotion_distribution(conversation_text)
        answer_emotions = self._emotion_distribution(answer)
        emotion_alignment = self._distribution_similarity(convo_emotions, answer_emotions)

        strongest_msg = self._most_emotional_message(messages)
        local_alignment = self._similarity(answer, strongest_msg)

        names_negative_emotion = self._contains_any(
            answer,
            ["stressed", "discouraged", "sad", "upset", "terrible", "anxious", "worried", "frustrated"]
        )

        dismisses_emotion = self._contains_any(
            answer,
            ["overreacting", "not a big deal", "just move on", "too dramatic", "too sensitive"]
        )

        raw = (
            0.46 * emotion_alignment +
            0.22 * local_alignment
        )

        if names_negative_emotion:
            raw += 0.16

        if dismisses_emotion:
            raw -= 0.28

        if question_focus == "emotion":
            raw += 0.08

        raw = max(0.0, min(1.0, raw))
        return self._to_1_5(raw), emotion_alignment

    def _score_empathy(
        self,
        messages: List[str],
        question: str,
        answer: str,
        question_focus: str,
    ) -> int:
        conversation_text = "\n".join(messages)

        negative_intensity = self._negative_intensity(conversation_text)

        supportive_phrases = self._contains_any(
            answer,
            [
                "sounds stressed", "sounds upset", "sounds discouraged", "feels terrible",
                "seems stressed", "seems upset", "not fully sure", "unsure what to do",
                "discouraged", "stressed"
            ]
        )

        dismissive_phrases = self._contains_any(
            answer,
            [
                "overreacting", "not a big deal", "just move on", "too sensitive",
                "dramatic", "should get over it"
            ]
        )

        judgmental_phrases = self._contains_any(
            answer,
            [
                "their fault", "they caused this", "they brought it on themselves",
                "lazy", "careless"
            ]
        )

        neutral_descriptive = self._zero_shot_label_score(
            answer,
            labels=["neutral and descriptive", "dismissive", "judgmental"],
            target_label="neutral and descriptive"
        )

        supportive_model = self._zero_shot_label_score(
            answer,
            labels=["supportive and understanding", "neutral", "dismissive"],
            target_label="supportive and understanding"
        )

        raw = 0.10 + 0.10 * negative_intensity + 0.18 * neutral_descriptive + 0.18 * supportive_model

        if supportive_phrases:
            raw += 0.28

        if dismissive_phrases:
            raw -= 0.40

        if judgmental_phrases:
            raw -= 0.18

        if question_focus == "empathy":
            raw += 0.06

        raw = max(0.0, min(1.0, raw))
        return self._to_1_5(raw)

    def _score_blame(
        self,
        messages: List[str],
        question: str,
        answer: str,
        question_focus: str,
    ) -> int:
        direct_blame = self._contains_any(
            answer,
            [
                "their fault", "they caused this", "they brought it on themselves",
                "they should have known", "careless", "lazy"
            ]
        )

        minimizing_judgment = self._contains_any(
            answer,
            [
                "overreacting", "not a big deal", "just move on", "too sensitive", "dramatic"
            ]
        )

        neutral_descriptive = self._contains_any(
            answer,
            [
                "sounds stressed", "sounds discouraged", "seems unsure", "not fully sure",
                "feels", "sounds", "seems"
            ]
        )

        judgment_model = self._zero_shot_label_score(
            answer,
            labels=["judgmental", "neutral description", "supportive response"],
            target_label="judgmental"
        )

        raw = 0.06 + 0.18 * judgment_model

        if direct_blame:
            raw += 0.38

        if minimizing_judgment:
            raw += 0.26

        if neutral_descriptive:
            raw -= 0.18

        if question_focus == "blame":
            raw += 0.05

        raw = max(0.0, min(1.0, raw))
        return self._to_1_5(raw)

    def _score_certainty(
        self,
        messages: List[str],
        question: str,
        answer: str,
        question_focus: str,
    ) -> int:
        hedges = self._count_matches(
            answer,
            [
                "i think", "maybe", "might", "could", "possibly", "perhaps",
                "not sure", "seems", "probably", "i guess", "appears", "kind of", "sort of"
            ]
        )

        assertive = self._count_matches(
            answer,
            [
                "should", "definitely", "clearly", "certainly", "for sure",
                "absolutely", "must", "need to", "obviously", "just move on",
                "not a big deal", "overreacting"
            ]
        )

        uncertainty_content = self._contains_any(
            answer,
            ["not fully sure", "unsure", "uncertain", "doesn't know what to do", "not sure what to do"]
        )

        confident_model = self._zero_shot_label_score(
            answer,
            labels=["confident and assertive", "hesitant and uncertain"],
            target_label="confident and assertive"
        )

        uncertain_model = self._zero_shot_label_score(
            answer,
            labels=["confident and assertive", "hesitant and uncertain"],
            target_label="hesitant and uncertain"
        )

        raw = 0.50 + 0.16 * confident_model - 0.16 * uncertain_model
        raw += 0.12 * min(assertive, 2)
        raw -= 0.08 * min(hedges, 2)

        # If the answer explicitly says the person is unsure, that should reduce certainty,
        # but not all the way to 1 unless the whole answer is hesitant.
        if uncertainty_content:
            raw -= 0.14

        if question_focus == "certainty":
            raw += 0.06

        raw = max(0.0, min(1.0, raw))
        return self._to_1_5(raw)

    # ------------------------------------------------------------
    # Helpers
    # ------------------------------------------------------------

    def _similarity(self, text_a: str, text_b: str) -> float:
        emb = self.embedder.encode([text_a, text_b], normalize_embeddings=True)
        cosine = float(np.dot(emb[0], emb[1]))
        return max(0.0, min(1.0, (cosine + 1.0) / 2.0))

    def _emotion_distribution(self, text: str) -> Dict[str, float]:
        raw = self.emotion_classifier(text)
        if len(raw) == 1 and isinstance(raw[0], list):
            raw = raw[0]

        scores: Dict[str, float] = {}
        for item in raw:
            label = item["label"].lower().strip()
            scores[label] = float(item["score"])

        total = sum(scores.values()) or 1.0
        return {k: v / total for k, v in scores.items()}

    def _distribution_similarity(self, dist_a: Dict[str, float], dist_b: Dict[str, float]) -> float:
        keys = set(dist_a.keys()) | set(dist_b.keys())
        overlap = sum(min(dist_a.get(k, 0.0), dist_b.get(k, 0.0)) for k in keys)
        return max(0.0, min(1.0, overlap))

    def _negative_intensity(self, text: str) -> float:
        dist = self._emotion_distribution(text)
        return max(
            0.0,
            min(1.0, dist.get("sadness", 0.0) + dist.get("fear", 0.0) + dist.get("anger", 0.0))
        )

    def _most_emotional_message(self, messages: List[str]) -> str:
        best_msg = messages[0]
        best_intensity = -1.0
        for msg in messages:
            dist = self._emotion_distribution(msg)
            intensity = (
                dist.get("sadness", 0.0) +
                dist.get("fear", 0.0) +
                dist.get("anger", 0.0) +
                dist.get("joy", 0.0) +
                dist.get("surprise", 0.0) -
                dist.get("neutral", 0.0)
            )
            if intensity > best_intensity:
                best_intensity = intensity
                best_msg = msg
        return best_msg

    def _zero_shot_label_score(self, text: str, labels: List[str], target_label: str) -> float:
        result = self.zero_shot(text, candidate_labels=labels, multi_label=True)
        scores = dict(zip(result["labels"], result["scores"]))
        total = sum(scores.values()) or 1.0
        normalized = {k: v / total for k, v in scores.items()}
        return float(normalized.get(target_label, 0.0))

    def _question_focus_alignment(self, question: str, answer: str) -> float:
        focus = self.detect_question_focus(question)
        focus_descriptions = {
            "intent": "The question is asking about motive, cause, meaning, or plan.",
            "emotion": "The question is asking about feelings, stress, mood, or emotional state.",
            "empathy": "The question is asking whether the answer sounds caring and understanding.",
            "blame": "The question is asking about fault, criticism, or judgment.",
            "certainty": "The question is asking how confident or unsure the answer sounds.",
            "general": "The question is broadly asking what is happening in the conversation.",
        }
        desc = focus_descriptions[focus]
        return 0.55 * self._similarity(answer, desc) + 0.45 * self._similarity(answer, question)

    def _contains_any(self, text: str, phrases: List[str]) -> bool:
        t = text.lower()
        return any(p.lower() in t for p in phrases)

    def _count_matches(self, text: str, phrases: List[str]) -> int:
        t = text.lower()
        return sum(1 for p in phrases if p.lower() in t)

    def _to_1_5(self, value_0_to_1: float) -> int:
        value = max(0.0, min(1.0, value_0_to_1))
        return int(round(1 + value * 4))


if __name__ == "__main__":
    ai = ResponseScoringAI()

    sample_1 = ConversationInput(
        messages=[
            "Person A: I failed my exam and I feel terrible.",
            "Person B: Why do you think that happened?",
            "Person A: I didn't study enough and now I'm stressed.",
            "Person B: What are you going to do next?"
        ],
        question="Based on the conversation, how is Person A feeling and how certain does this answer sound?",
        answer="Person A sounds stressed and discouraged. I think they are not fully sure what to do next."
    )

    sample_2 = ConversationInput(
        messages=[
            "Person A: I failed my exam and I feel terrible.",
            "Person B: Why do you think that happened?",
            "Person A: I didn't study enough and now I'm stressed.",
            "Person B: What are you going to do next?"
        ],
        question="Based on the conversation, how is Person A feeling and how certain does this answer sound?",
        answer="Person A seems stressed, disappointed, and upset about failing the exam. The answer sounds fairly certain because the conversation directly shows how they feel."
    )

    print("Sample 1:")
    print(ai.analyze(sample_1))
    print()
    print("Sample 2:")
    print(ai.analyze(sample_2))