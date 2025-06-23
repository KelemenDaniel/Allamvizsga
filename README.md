# VR Educational Escape Room

A digital virtual reality escape room designed for educational purposes, combining immersive VR experiences with cloud-based backend infrastructure.

---

## Overview

This project creates an engaging educational experience through virtual reality escape room gameplay. Students can explore, solve puzzles, and learn in an immersive 3D environment while their progress and interactions are tracked through a robust backend system.

---

## Architecture

### Frontend - VR Application

- **Platform**: Unity 2019.2.13f1  
- **Technology**: Virtual Reality escape room experience  
- **Purpose**: Immersive educational gameplay and user interaction  

### Backend - API Service

- **Framework**: Python FastAPI  
- **Deployment**: Docker container hosted on Microsoft Azure  
- **Purpose**: Handles game logic, and data processing  

### Database

- **Type**: MySQL  
- **Hosting**: Microsoft Azure Database for MySQL  
- **Purpose**: Stores stories and puzzles

---

## Features

- Immersive VR escape room environment  
- Educational puzzle and challenge integration  
- Scalable backend architecture  
---

## Technical Stack

| Component      | Technology     | Version/Details              |
|----------------|----------------|------------------------------|
| VR Client      | Unity          | 2019.2.13f1                  |
| Backend API    | Python FastAPI | Latest                       |
| Database       | MySQL          | Azure Database for MySQL     |
| Containerization | Docker        | Latest                       |
| Cloud Platform | Microsoft Azure | Container deployment         |

---

## Getting Started

### Prerequisites

- Unity 2019.2.13f1 or compatible version  
- VR headset and compatible hardware  
- Docker (for local backend development)  
- Python 3.7+ (for backend development)  

### Backend Setup

1. Clone the repository  
2. Navigate to the backend directory  
3. Build the Docker container:
   ```bash
   docker build -t vr-escape-room-api .
4. Configure environment variables for Azure and MySQL connections
5. Deploy to Azure Container Instances or Azure App Service

### Unity VR Client Setup

1. Open the project in Unity 2019.2.13f1
2. Update API endpoints to point to your deployed backend
3. Build and deploy to your VR platform

### Database Setup

1. Create MySQL database instance on Azure
2. Run migration scripts to set up tables
3. Configure connection strings in the backend application

---

## Configuration

### Environment Variables

The backend requires the following environment variables:

- `DATABASE_URL`: MySQL connection string  
- `GOOGLE_API_KEY`: Secret key for API authentication  

---

### Unity Configuration

- Update API base URL in Unity project settings  
- Configure VR SDK settings for target hardware  
- Set up scene management for escape room progression  

---

## Educational Integration

This escape room is designed to support various educational objectives:

- Problem-solving and critical thinking skills  
- Adaptive difficulty based on student performance  

---

## Deployment

### Backend Deployment on Azure

The FastAPI backend is containerized and deployed on Azure using:

- **Azure Container Instances** for simple deployment  
- **Azure App Service** for production scaling  
- **Azure Database for MySQL** for data persistence  

---

## Development

### Local Development

- Run MySQL locally or use Azure development database  
- Start the FastAPI backend locally:
  ```bash
  uvicorn main:app --reload
- Open Unity project and configure local API endpoints
- Test VR functionality with your development headset

## Acknowledgments

- Built with **Unity** for VR development  
- Powered by **FastAPI** for high-performance backend services  
- Hosted on **Microsoft Azure** for reliable cloud infrastructure  
- Educational design inspired by immersive learning research


