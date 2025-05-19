import hashlib
import os
from dotenv import load_dotenv
from sqlalchemy import create_engine, func
from sqlalchemy.orm import sessionmaker
from ORM import User, Story, Puzzle

load_dotenv()
database_url = os.getenv("DATABASE_URL")

engine = create_engine(database_url, pool_size=20)
connection = engine.connect()
Session = sessionmaker()


def get_all_users():
    local_session = Session(bind=engine)
    users = local_session.query(User).all()
    if users is None:
        return None
    return users


def hash_password(password: str):
    pwdhash = hashlib.sha256(password.encode('utf-8')).hexdigest()
    return pwdhash


def compare_password(input_password: str, db_password: str) -> bool:
    pwd = hash_password(input_password)
    if pwd == db_password:
        return True
    return False


def register_user(user_data: dict):
    local_session = Session(bind=engine)
    username = user_data['username']
    email = user_data['email']

    user_data['password'] = hash_password(user_data['password'])

    users = get_all_users()
    for u in users:
        existing = u.to_dict()
        if existing['email'] == email or existing['username'] == username:
            return False

    new_user = User(
        username=user_data['username'],
        password=user_data['password'],
        email=user_data['email']
    )

    local_session.add(new_user)
    local_session.commit()

    return True


def login_user(email: str, password: str):
    users = get_all_users()

    for user in users:
        user_data = user.to_dict()
        if user_data['email'] == email and compare_password(password, user_data['password']):
            return user.id
    return False


def add_story(story: dict):
    local_session = Session(bind=engine)
    new_story = Story(
        description=story['description'],
        difficulty=story['difficulty'],
        type=story['type']
    )
    local_session.add(new_story)
    local_session.commit()

    return new_story.id


def add_puzzle(puzzle: dict):
    local_session = Session(bind=engine)
    new_puzzle = Puzzle(
        story_id=puzzle['story_id'],
        question=puzzle['question'],
        possible_answers=puzzle['possible_answers'],
        correct_answer=puzzle['correct_answer']
    )
    local_session.add(new_puzzle)
    local_session.commit()


def get_story_by_id(story_id: int):
    local_session = Session(bind=engine)
    story = local_session.get(Story, story_id)
    if story is None:
        return None
    return story.to_dict()


def get_puzzles_by_story_id(story_id: int):
    local_session = Session(bind=engine)
    puzzles = local_session.query(Puzzle).filter_by(story_id=story_id).all()
    if puzzles is None:
        return None
    return [puzzle.to_dict() for puzzle in puzzles]


def get_three_random_stories():
    local_session = Session(bind=engine)
    stories = local_session.query(Story).order_by(func.random()).limit(3).all()
    if stories is None:
        return None
    return [story.to_dict() for story in stories]

print(login_user("asd@hjsdg.com", "asd12345678"))