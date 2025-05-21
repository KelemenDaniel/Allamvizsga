# Dockerfile

FROM python:3.10

WORKDIR /app

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY . .

# Make sure .env is copied inside the container
COPY .env .env

CMD ["uvicorn", "endpoints:app", "--host", "0.0.0.0", "--port", "80"]
