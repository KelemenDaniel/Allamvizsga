from sqlalchemy import Column, Integer, String, Text, JSON
from sqlalchemy.ext.declarative import declarative_base

Base = declarative_base()


class Story(Base):
    __tablename__ = 'stories'

    id = Column(Integer, primary_key=True)
    description = Column(Text, nullable=False)
    difficulty = Column(String(50), nullable=False)
    type = Column(String(50), nullable=False)

    def __repr__(self):
        return f"Story id={self.id} description={self.description} difficulty={self.difficulty}"

    def get_description(self):
        return self.description

    def set_description(self, value):
        self.description = value

    def get_diffuculty(self):
        return self.difficulty

    def set_difficulty(self, value):
        self.difficulty = value

    def to_dict(self):
        return {
            "id": self.id,
            "description": self.description,
            "difficulty": self.difficulty,
            "type": self.type
        }


class Puzzle(Base):
    __tablename__ = 'puzzles'

    id = Column(Integer, primary_key=True)
    story_id = Column(Integer, nullable=False)
    question = Column(Text, nullable=False)
    possible_answers = Column(JSON, nullable=False)
    correct_answer = Column(String(255), nullable=False)

    def __repr__(self):
        return (f"Puzzle id={self.id} story_id={self.story_id} question={self.question} "
                f"possible_answers={self.possible_answers} correct_answer={self.correct_answer}")

    def get_question(self):
        return self.question

    def set_question(self, value):
        self.question = value

    def get_possible_answers(self):
        return self.possible_answers

    def set_possible_answers(self, value):
        self.possible_answers = value

    def get_correct_answer(self):
        return self.correct_answer

    def set_correct_answer(self, value):
        self.correct_answer = value

    def to_dict(self):
        return {
            "id": self.id,
            "story_id": self.story_id,
            "question": self.question,
            "possible_answers": self.possible_answers,
            "correct_answer": self.correct_answer
        }


class User(Base):
    __tablename__ = 'users'

    id = Column(Integer, primary_key=True)
    username = Column(String(50), nullable=False)
    password = Column(String(100), nullable=False)
    email = Column(String(100), nullable=False)

    def __repr__(self):
        return f"User id={self.id} username={self.username} password={self.password} email={self.email}"

    def get_username(self):
        return self.username

    def set_username(self, value):
        self.username = value

    def get_password(self):
        return self.password

    def set_password(self, value):
        self.password = value

    def get_email(self):
        return self.email

    def set_email(self, value):
        self.email = value

    def to_dict(self):
        return {
            "id": self.id,
            "username": self.username,
            "password": self.password,
            "email": self.email
        }

