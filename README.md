# ImageMetaAI

Local AI-powered image analysis and metadata generation for stock photography.

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Requirements](#requirements)
- [Ollama Setup](#ollama-setup)
- [GPS-Based Location Recognition](#gps-based-location-recognition)
- [Project Structure](#project-structure)
- [Development](#development)
- [Testing](#testing)
- [Git](#git)
- [Privacy](#privacy)

## Overview

ImageMetaAI is a local Windows desktop application for analyzing images and generating stock photography metadata using locally running AI models.

The application is designed to keep AI image analysis and metadata generation local. Images are not uploaded to external AI services.

## Features

- Scan a local folder for images.
- Analyze images using a local vision model.
- Generate stock photography titles, keywords, and Shutterstock categories.
- Review and edit generated metadata before export.
- Process individual images or batches of images.
- Read GPS coordinates from image EXIF metadata.
- Mark images containing GPS coordinates in the image list.
- Optionally resolve GPS coordinates to location names using OpenStreetMap Nominatim.
- Use resolved location information as additional context for metadata generation.
- Export metadata to a Shutterstock-compatible CSV file.
- Keep original image files unchanged.

## Requirements

- Windows 10 or Windows 11
- .NET 9 SDK
- Ollama
- Compatible hardware for running local AI models
- `qwen2.5vl:7b` for image analysis and metadata generation

## Ollama Setup

ImageMetaAI uses Ollama as its local AI runtime.

The currently configured model is:

- `qwen2.5vl:7b` for image analysis and metadata generation

Install the model using:

```powershell
ollama pull qwen2.5vl:7b
```

Make sure Ollama is installed and running, and that the model is available locally.

The application connects to the local Ollama API at:

```text
http://localhost:11434
```

A different model may be configured for metadata generation in the future, particularly to improve keyword and tag quality.

## GPS-Based Location Recognition

ImageMetaAI reads GPS coordinates from image EXIF metadata.

Images containing GPS coordinates are marked with a globe icon in the image list.

The **Recognize locations from GPS** option enables reverse geocoding through OpenStreetMap Nominatim. When enabled, the resolved location can be included as additional context for metadata generation.

- Location recognition is optional.
- Images without GPS coordinates can still be analyzed normally.
- If location lookup fails, image analysis and metadata generation can continue.
- GPS metadata is read locally; original image files are not modified.
- Location lookup requires an internet connection and is subject to the Nominatim usage policy.

## Project Structure

The solution is organized into application source code and automated tests.

- `src/ImageMetaAI/` — WPF desktop application
- `tests/ImageMetaAI.Tests/` — automated tests

The application uses C#, .NET, WPF, Ollama, and MetadataExtractor.

## Development

Open the solution in Visual Studio or Visual Studio Code with the .NET SDK installed.

Build the project:

```powershell
dotnet build
```

Run the application:

```powershell
dotnet run --project src/ImageMetaAI/ImageMetaAI.csproj
```

## Testing

Run the automated tests:

```powershell
dotnet test
```

## Git

Check the working tree:

```powershell
git status
```

Review changes:

```powershell
git diff
```

Create a commit:

```powershell
git add .
git commit -m "Describe the change"
```

## Privacy

- AI image analysis and metadata generation use the locally running Ollama service.
- Original image files are not modified.
- GPS coordinates are read from local EXIF metadata.
- If GPS-based location recognition is enabled, coordinates are sent to OpenStreetMap Nominatim for reverse geocoding.
- Location lookup is optional and requires an internet connection.
- ImageMetaAI does not automatically upload images to stock photography platforms.