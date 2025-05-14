from fastapi import FastAPI, HTTPException
import database_utils as db
import StoryGeneration as gen
from models import *

app = FastAPI()

@app.post("/register")
def register(user: UserIn):
    success = db.register_user(user.to_dict())
    if not success:
        raise HTTPException(status_code=400, detail="Username or email already exists.")
    return {"message": "User registered successfully"}


@app.post("/login")
def login(login_data: LoginIn):
    user_id = db.login_user(login_data.email, login_data.password)
    if user_id is True:
        raise HTTPException(status_code=401, detail="Invalid credentials")
    return {"message": "Login successful", "user_id": user_id}


@app.post("/story")
def create_story(story: StoryIn):
    story_id = db.add_story(story.to_dict())
    return {"message": "Story created", "story_id": story_id}


@app.get("/story/{story_id}")
def get_story(story_id: int):
    story = db.get_story_by_id(story_id)
    if not story:
        raise HTTPException(status_code=404, detail="Story not found")
    return story


@app.get("/stories/random")
def get_random_stories():
    stories = db.get_three_random_stories()
    return stories


@app.post("/puzzle")
def create_puzzle(puzzle: PuzzleIn):
    db.add_puzzle(puzzle.to_dict())
    return {"message": "Puzzle added"}


@app.get("/puzzles/{story_id}")
def get_puzzles(story_id: int):
    puzzles = db.get_puzzles_by_story_id(story_id)
    return puzzles


@app.get("/generate/mathematics/{difficulty}")
def generate_math_story(difficulty: str):
    return gen.mathematical_story(difficulty)


@app.get("/generate/informatics/{difficulty}")
def generate_informatics_story(difficulty: str):
    return gen.informatics_story(difficulty)


@app.get("/generate/literature/{difficulty}")
def generate_literature_story(difficulty: str):
    return gen.hungarian_literature_story(difficulty)
