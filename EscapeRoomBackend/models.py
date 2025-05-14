from typing import List
from pydantic import BaseModel, EmailStr


class UserIn(BaseModel):
    username: str
    password: str
    email: EmailStr


class LoginCredentials(BaseModel):
    email: EmailStr
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

