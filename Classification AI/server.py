from typing import List
from fastapi import FastAPI
from pydantic import BaseModel
import uvicorn

from Classification_AI import ConversationInput, ResponseScoringAI

app = FastAPI(title="Response Scoring API")

# Load once when the server starts
scorer = ResponseScoringAI()


class ScoreRequest(BaseModel):
    messages: List[str]
    question: str
    answer: str


@app.get("/")
def root():
    return {"status": "ok", "message": "Response scoring API is running."}


@app.post("/score")
def score(req: ScoreRequest):
    sample = ConversationInput(
        messages=req.messages,
        question=req.question,
        answer=req.answer,
    )
    return scorer.analyze(sample)


if __name__ == "__main__":
    uvicorn.run(app, host="127.0.0.1", port=8000)