from google import genai
from dotenv import load_dotenv
import os


def load_client():
    load_dotenv()
    api_key = os.getenv("GOOGLE_API_KEY")
    return genai.Client(api_key=api_key)


def mathematical_story(difficulty):
    client = load_client()
    response = client.models.generate_content(
        model="gemini-2.0-flash", contents="Dolgozz ki egy szabaduloszoba tortenetet, edukacios cellal, amelyben "
                                           f"pontosan 5 rejtvenyt teszel fel {difficulty} nehezsegu matematika temaban, "
                                           "mindegyik rejtvenynek legyen 4 valaszlehetosege, a tortentetet es a "
                                           "rejtvenyeket illetve megoldasokat json formatumban add meg. A jsonban "
                                           "csak egy story, egy puzzles[puzzle[question(csak konkretan a "
                                           "kerdest tartalmazza), possible answers, correct answer]]. Csak a JSON "
                                           "legyen a valaszodban."
    )
    return response.text


def informatics_story(difficulty):
    client = load_client()
    response = client.models.generate_content(
        model="gemini-2.0-flash", contents="Dolgozz ki egy szabaduloszoba tortenetet, edukacios cellal, amelyben "
                                           f"pontosan 5 rejtvenyt teszel fel {difficulty} nehezsegu programozas  "
                                           "temaban, mindegyik rejtvenynek legyen 4 valaszlehetosege, a tortentetet "
                                           "es a rejtvenyeket illetve megoldasokat json formatumban add meg. A jsonban "
                                           "csak egy story, egy puzzles[puzzle[question(csak konkretan a "
                                           "kerdest tartalmazza), possible answers, correct answer]]. "
                                           "Csak a JSON legyen a valaszodban."
    )
    return response.text


def hungarian_literature_story(difficulty):
    client = load_client()
    response = client.models.generate_content(
        model="gemini-2.0-flash", contents="Dolgozz ki egy szabaduloszoba tortenetet, edukacios cellal, amelyben "
                                           f"pontosan 5 rejtvenyt teszel fel {difficulty} nehezsegu magyar irodalom "
                                           "temaban, mindegyik rejtvenynek legyen 4 valaszlehetosege, a tortentetet "
                                           "es a rejtvenyeket illetve megoldasokat json formatumban add meg. A jsonban "
                                           "csak egy tortenet, egy rejtvenyek[rejtveny[kerdes(csak konkretan a "
                                           "kerdest tartalmazza), valaszlehetosegek, helyes megoldas]]. Csak a JSON "
                                           "legyen a valaszodban."
    )
    return response.text


if __name__ == '__main__':
    # mathematical_story("konnyu")
    print(informatics_story("konnyu"))
    # hungarian_literature_story("konnyu")
