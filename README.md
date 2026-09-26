# ImageMetaAI

Local AI-powered image analysis and metadata generation for stock photography.

## Table of Contents

- [Overview](#overview)
- [Requirements](#requirements)
- [Ollama Setup](#ollama-setup)
- [Project Structure](#project-structure)
- [Development](#development)
- [Testing](#testing)
- [Git](#git)

## Overview

ImageMetaAI is a local Windows desktop application for analyzing images
and generating stock photography metadata with locally running AI models.

The application is designed to keep image processing local.
Images are not uploaded to external AI services.

## Requirements

- Windows 10 or Windows 11
- .NET 9 SDK
- Ollama
- NVIDIA GPU with sufficient VRAM
- Qwen2.5-VL 7B
- Gemma 4 26B
- ExifTool

## Ollama Setup

ImageMetaAI uses Ollama as the local AI runtime.

Required models:

- `qwen2.5vl:7b` for image analysis
- `gemma4:26b` for metadata generation

The application is intended to start Ollama automatically when required.

If Ollama is not installed, it must be installed before using ImageMetaAI.
