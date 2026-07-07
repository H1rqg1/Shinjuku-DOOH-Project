from datetime import datetime
from pathlib import Path
from threading import Lock
from typing import Any, Dict, List, Optional
import json

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel


BASE_DIR = Path(__file__).resolve().parent
DATA_DIR = BASE_DIR / "data"
DATA_PATH = DATA_DIR / "encounters.json"

data_lock = Lock()

app = FastAPI(title="DOOH Encounter Server")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


class Encounter(BaseModel):
    my_id: str
    target_id: str
    timestamp: Optional[str] = None


def ensure_data_file() -> None:
    DATA_DIR.mkdir(parents=True, exist_ok=True)
    if not DATA_PATH.exists():
        write_encounters([])


def read_encounters() -> List[Dict[str, Any]]:
    ensure_data_file()

    try:
        with DATA_PATH.open("r", encoding="utf-8") as file:
            data = json.load(file)
    except (json.JSONDecodeError, OSError):
        return []

    if isinstance(data, dict):
        encounters = data.get("encounters", [])
    else:
        encounters = data

    if not isinstance(encounters, list):
        return []

    return [item for item in encounters if isinstance(item, dict)]


def write_encounters(encounters: List[Dict[str, Any]]) -> None:
    DATA_DIR.mkdir(parents=True, exist_ok=True)
    with DATA_PATH.open("w", encoding="utf-8") as file:
        json.dump(encounters, file, ensure_ascii=False, indent=2)


def encounter_to_dict(encounter: Encounter) -> Dict[str, Any]:
    if hasattr(encounter, "model_dump"):
        payload = encounter.model_dump()
    else:
        payload = encounter.dict()

    if not payload.get("timestamp"):
        payload["timestamp"] = datetime.now().astimezone().isoformat(timespec="seconds")

    return payload


@app.get("/")
def root() -> Dict[str, Any]:
    return {
        "message": "DOOH Encounter Server is running",
        "endpoints": {
            "save": "POST /encounter",
            "list": "GET /encounters",
            "reset": "DELETE /encounters",
        },
    }


@app.post("/encounter")
def save_encounter(encounter: Encounter) -> Dict[str, Any]:
    payload = encounter_to_dict(encounter)

    with data_lock:
        encounters = read_encounters()
        encounters.append(payload)
        write_encounters(encounters)

    return {
        "message": "saved",
        "count": len(encounters),
        "encounter": payload,
    }


@app.get("/encounters")
def list_encounters() -> Dict[str, Any]:
    with data_lock:
        encounters = read_encounters()

    return {"encounters": encounters}


@app.delete("/encounters")
def reset_encounters() -> Dict[str, Any]:
    with data_lock:
        write_encounters([])

    return {
        "message": "reset",
        "encounters": [],
    }
