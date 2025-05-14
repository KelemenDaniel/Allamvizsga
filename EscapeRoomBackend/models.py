from typing import List
from pydantic import BaseModel


class UserIn(BaseModel):
    username: str
    password: str
    email: str


class LoginIn(BaseModel):
    email: str
    password: str


class StoryIn(BaseModel):
    description: str
    difficulty: str
    type: str


class PuzzleIn(BaseModel):
    story_id: int
    question: str
    possible_answers: List[str]
    correct_answer: str