<div align="center">

# PartnersApp — שותפים לדרך

**A social hiking platform for the Society for the Protection of Nature in Israel (החברה להגנת הטבע)**

Final-year graduation project · React Native (Expo) · ASP.NET Core · SQL Server · Google Gemini · Firebase

</div>

---

## Table of Contents

- [About The Project](#about-the-project)
- [Key Features](#key-features)
- [Tech Stack](#tech-stack)
- [Project Architecture](#project-architecture)
- [Getting Started & Installation](#getting-started--installation)
- [Security Notice](#-security-notice--required-manual-configuration)
- [API Overview](#api-overview)
- [License](#license)

---

## About The Project

**PartnersApp ("שותפים לדרך")** is a cross-platform mobile application built for the *Society for the Protection of Nature in Israel*. It addresses a concrete problem the organization faces: people want to join guided nature trips, but many hesitate to sign up alone, drop out before the trip date, or never connect with the community that forms around these hikes.

The app turns a one-off trip registration into an ongoing social experience:

- **Discovery & registration** — users browse the organization's catalog of nature trips (category, location, date, difficulty) and register directly from their phone.
- **Finding a partner** — a two-layer matching engine suggests other hikers to travel with, based on declared profile data (shared interests, city, family status) and on *behavioral taste* inferred from the trips each user has rated. Every suggestion is returned with a match score and a human-readable list of reasons ("3 shared interests", "you're both from Haifa").
- **Meeting on the trail** — a real-time proximity map, powered by SignalR, shows other consenting users within a 1 km radius during a trip, with an explicit consent flow: send an interaction request → the other side accepts → a private, ephemeral chat channel opens. Rate limiting, blocking, and channel-membership checks are enforced server-side.
- **Community** — a feed of posts with images, likes, and comments, plus a reporting mechanism for inappropriate content.
- **AI assistance** — a Hebrew-speaking chatbot ("פארטנר") answers questions grounded in the user's own trip data, and AI services score feedback sentiment and flag toxic content.
- **Administration** — a management dashboard for the organization's staff: KPIs, RSVP-vs-attendance analytics, sentiment distribution, chatbot efficiency, user risk scoring, content moderation, and AI-generated push-notification campaigns.

The entire user-facing product is **Hebrew-first and RTL**, matching the organization's audience.

---

## Key Features

| Area | Capability |
|---|---|
| **Authentication** | JWT-based registration & login, role separation (`User` / `Admin`), account blocking |
| **Trips** | Browse, filter, register, personal trip history, admin CRUD |
| **Smart Matching** | Two-layer scoring: profile similarity (0–100) + behavioral taste from trip ratings (0–100) |
| **Connections** | Friend-request flow (`none` / `pending` / `accepted` / `rejected`) with a personal message |
| **Live Proximity Map** | SignalR hub broadcasting locations within a 1 km radius (Haversine), consent-gated private channels, blocking, and per-user rate limiting |
| **Real-Time Chat** | 1-to-1 messaging plus ephemeral in-session channels via SignalR |
| **Community Feed** | Posts with multi-image upload, likes, comments, content reporting |
| **AI Chatbot** | Gemini-powered Hebrew assistant with trip context injection and a multi-model fallback chain |
| **Sentiment Analysis** | Feedback classified as `Positive` / `Negative` / `Neutral` / `Urgent_Negative` with a 0–100 score and a short summary |
| **Toxicity Scoring** | HuggingFace `unitary/toxic-bert` scoring for moderation triage |
| **Smart Push Generator** | Gemini generates Hebrew push copy per campaign goal (signup, reminder, equipment, hype, feedback) |
| **Push Notifications** | Firebase Cloud Messaging via Firebase Admin SDK + Expo Notifications |
| **Admin Dashboard** | KPIs, attendance analytics, sentiment charts, bot efficiency, grouped reports, user risk scoring, moderation actions |
| **Route Planning** | Saveable routes with ordered waypoints, distance, and a shareable token |

---

## Tech Stack

### Frontend — Mobile (React Native / Expo)

| Technology | Purpose |
|---|---|
| **React Native 0.81** + **React 19** | Core UI framework |
| **Expo SDK 54** (New Architecture enabled) | Build tooling, native modules, OTA-ready pipeline |
| **Expo Router 6** | File-based routing with typed routes |
| **TypeScript 5.9** | Type safety (mixed `.tsx` / `.jsx` codebase) |
| **@microsoft/signalr** | Real-time client for location, presence, and chat |
| **react-native-maps** + **react-native-map-clustering** | Map rendering and marker clustering |
| **expo-location** | Foreground GPS tracking |
| **expo-notifications** | Push notification registration & handling |
| **firebase (Web SDK)** | Client-side Firebase integration |
| **expo-image-picker** / **expo-image** | Image selection and optimized rendering |
| **react-native-reanimated 4** + **react-native-worklets** | Animations |
| **lottie-react-native** | Splash and loading animations |
| **@expo-google-fonts/rubik** | Hebrew-friendly typography |
| **@react-native-async-storage/async-storage** | JWT and session persistence |
| **expo-speech** | Text-to-speech for accessibility |
| **ESLint (eslint-config-expo)** | Linting |

### Backend — API (ASP.NET Core)

| Technology | Purpose |
|---|---|
| **ASP.NET Core Web API** (`PartnersWebApi`) | REST API host |
| **SignalR** | `LocationHub` (`/hubs/location`) for live location, presence, and channels |
| **JWT Bearer Authentication** | Token issuance and validation, including SignalR query-string tokens |
| **Swagger / Swashbuckle** | Interactive API documentation with Bearer auth |
| **Dapper** + **System.Data.SqlClient** | Data access (Dapper for map/route queries, ADO.NET for stored procedures) |
| **IHttpClientFactory** | Named, timeout-bounded clients for `gemini` and `huggingface` |
| **Hosted Service** (`StaleSessionCleaner`) | Background cleanup of stale presence sessions |
| **Singleton `PresenceStore`** | In-memory presence, blocking, channels, and rate limiting |
| **Static File Middleware** | Serving user-uploaded images from `/Uploads` |
| **CORS Policy** (`AllowReactApp`) | Explicit origin allow-list with credentials |

### Database

| Technology | Purpose |
|---|---|
| **Microsoft SQL Server** | Primary relational store |
| **Stored Procedures** (`SP_*_Nature`) | All core business queries — registration, trips, feedback, community, notifications, chat, admin KPIs, and matching (`SP_GetMatchingData_Nature`) |

Core tables include `Users`, `Trips`, `UserTrips`, `Feedbacks`, `Posts`, `Comments`, `Reports`, `Notifications`, `ChatSessions`, `Messages`, `Connections`, `Routes`, `Waypoints`, and `LiveLocations`.

### AI Services

| Service | Model(s) | Used For |
|---|---|---|
| **Google Gemini API** | `gemini-2.5-flash`, `gemini-2.0-flash`, `gemini-2.0-flash-lite`, `gemini-2.5-flash-lite`, `gemini-3-flash-preview` | Hebrew chatbot, sentiment analysis, report summarization, smart push generation |
| **HuggingFace Inference API** | `unitary/toxic-bert` | Toxicity scoring for community moderation |

> Both AI integrations implement an **automatic fallback chain** — on `429` (quota), `503` (unavailable), or `404`, the next model in the list is tried, so a free-tier quota exhaustion does not break the feature.

### Notifications

| Technology | Purpose |
|---|---|
| **Firebase Admin SDK** (`FirebaseAdmin`, `Google.Apis.Auth`) | Server-side push dispatch |
| **Firebase Cloud Messaging (FCM)** | Delivery channel |
| **Expo Notifications** | Device token registration and client-side display |

---

## Project Architecture

The repository is split into two independent applications: a **React Native client** and an **ASP.NET Core API**.

```
PartnersApp/
│
├── Frontend/                       # React Native + Expo client
│   ├── app/                        # Expo Router — file-based routing
│   │   ├── _layout.jsx             # Root layout, providers, splash
│   │   ├── index.jsx               # Entry / redirect
│   │   ├── login.jsx               # Authentication
│   │   ├── register.jsx
│   │   ├── (tabs)/                 # Main tab navigator
│   │   │   ├── _layout.jsx
│   │   │   ├── hub.jsx             # Home hub
│   │   │   ├── mytrips.jsx         # User's trips
│   │   │   ├── community.jsx       # Social feed
│   │   │   ├── chat.jsx            # AI chatbot
│   │   │   ├── profile.jsx
│   │   │   └── dashboard.jsx       # Admin-only dashboard
│   │   ├── trips/[tripId].jsx      # Dynamic trip details
│   │   ├── chats/[otherId].jsx     # 1-to-1 conversation
│   │   ├── similar.jsx             # Smart matching results
│   │   ├── requests.jsx            # Connection requests
│   │   ├── proximity.jsx           # Live proximity screen
│   │   └── map.jsx                 # Routes & waypoints map
│   │
│   ├── components/                 # Reusable UI
│   │   ├── AppHeader.jsx
│   │   ├── TripCard.jsx
│   │   ├── PostCard.jsx
│   │   ├── ProximityMap.tsx
│   │   ├── Chatpanel.tsx
│   │   ├── RecommendationModal.jsx
│   │   ├── LoadingScreen.jsx / AnimatedSplash.jsx
│   │   └── themed-text.tsx, themed-view.tsx, ...
│   │
│   ├── hooks/                      # Custom logic hooks
│   │   ├── useProximityHub.ts      # SignalR connection lifecycle
│   │   ├── useLocation.ts          # GPS subscription
│   │   ├── useSmartPush.js         # AI push generation
│   │   ├── useReportSummary.js     # AI report summarization
│   │   ├── useReportSeverity.js    # Report severity heuristics
│   │   ├── useUserRiskScoring.js   # Admin risk scoring
│   │   └── use-color-scheme.ts, use-theme-color.ts
│   │
│   ├── assets/                     # Images, icons, Lottie animations
│   ├── app.json                    # Expo configuration
│   ├── package.json
│   └── tsconfig.json
│
└── Backend/                        # PartnersWebApi — ASP.NET Core
    ├── Controllers/
    │   ├── AuthController.cs           # Register / login / JWT
    │   ├── UsersController.cs          # Profile, preferences, similar users
    │   ├── TripsController.cs          # Trip CRUD & registration
    │   ├── CommunityController.cs      # Posts, likes, comments, reports
    │   ├── ChatController.cs           # AI chatbot sessions & history
    │   ├── MessagesController.cs       # 1-to-1 messaging
    │   ├── ConnectionsController.cs    # Friend requests & status
    │   ├── NotificationsController.cs  # Push & in-app notifications
    │   ├── FeedbacksController.cs      # Feedback + sentiment
    │   ├── MapController.cs            # Routes, waypoints, live locations
    │   ├── FilesController.cs          # Image uploads
    │   ├── AiProxyController.cs        # Admin-only AI proxy (push / summary / toxicity)
    │   └── AdminController.cs          # Dashboard KPIs & moderation
    │
    ├── Hubs/
    │   └── LocationHub.cs              # SignalR: presence, proximity, channels, blocking
    │
    ├── Interfaces/                     # Repository & service contracts
    │   ├── IUsersRepository.cs, ITripsRepository.cs, ICommunityRepository.cs
    │   ├── IChatRepository.cs, IMessagesRepository.cs, IConnectionsRepository.cs
    │   ├── INotificationsRepository.cs, IFeedbacksRepository.cs
    │   ├── IDashboardRepository.cs
    │   └── IMapService.cs
    │
    ├── Repositories/                   # SQL Server implementations
    │   ├── SQLUsersRepository.cs       # incl. the matching algorithm
    │   ├── SQLTripsRepository.cs, SQLCommunityRepository.cs
    │   ├── SQLChatRepository.cs, SQLMessagesRepository.cs
    │   ├── SQLConnectionsRepository.cs, SQLNotificationsRepository.cs
    │   ├── SQLFeedbacksRepository.cs, DashboardRepository.cs
    │   └── MapService.cs
    │
    ├── Services/
    │   ├── GeminiAiService.cs          # Chat + sentiment (IChatAiService)
    │   ├── PresenceStore.cs            # In-memory presence / blocks / channels
    │   └── StaleSessionCleaner.cs      # Background cleanup
    │
    ├── Models/                         # Entities & DTOs
    │   ├── User.cs, Trip.cs, UserTrip.cs, Feedback.cs, Community.cs
    │   ├── Chatbot.cs, Notification.cs, Route.cs, Waypoint.cs, LiveLocation.cs
    │   └── SimilarUserDto.cs, RouteDto.cs, ChatDtos.cs, ConnectionDtos.cs, ...
    │
    ├── Uploads/                        # Runtime-generated image storage
    ├── Program.cs                      # DI, JWT, CORS, SignalR, Firebase, pipeline
    ├── appsettings.json                # ⚠️ Non-secret defaults only
    └── appsettings.Development.json    # ⚠️ NOT in the repository — create manually
```

### Request Flow

```
React Native (Expo)
        │  HTTPS  +  Bearer JWT
        ▼
ASP.NET Core Controllers ──► Repository Interfaces ──► SQL Server (Stored Procedures)
        │                            │
        │                            └──► Dapper (routes / live locations)
        │
        ├──► GeminiAiService / AiProxyController ──► Google Gemini API
        │                                        └──► HuggingFace Inference API
        │
        ├──► Firebase Admin SDK ──► FCM ──► Device
        │
        └──► SignalR LocationHub  ◄──WebSocket──►  React Native client
```

---

## Getting Started & Installation

### Prerequisites

| Requirement | Version |
|---|---|
| **.NET SDK** | 8.0 or later |
| **Node.js** | 20.x LTS or later |
| **npm** | 10.x or later |
| **Microsoft SQL Server** | 2019 or later (or a reachable remote instance) |
| **Visual Studio 2022** / **VS Code** | With the ASP.NET & C# workloads |
| **Expo Go** app | On a physical iOS/Android device (or an emulator) |
| **Google Gemini API key** | https://aistudio.google.com |
| **HuggingFace access token** | https://huggingface.co/settings/tokens |
| **Firebase project** | With a generated service-account key |

---

### 1. Clone the Repository

```bash
git clone <your-repository-url>
cd PartnersApp
```

---

### 2. Database Setup

1. Create a database on your SQL Server instance (e.g. `PartnersAppDb`).
2. Execute the provided schema and stored-procedure scripts (`SQL.txt` and the `SP_*_Nature.sql` files) against that database.
3. Verify that all stored procedures were created — the API depends on them exclusively; there is no code-first migration fallback.

---

### 3. Backend Setup (`PartnersWebApi`)

```bash
cd Backend
dotnet restore
```

Create the local configuration files described in the [Security Notice](#-security-notice--required-manual-configuration) below, then run:

```bash
dotnet run
```

The API starts on `https://localhost:7xxx` (see `launchSettings.json`). Swagger UI is available at:

```
https://localhost:7xxx/swagger
```

---

### 4. Frontend Setup (Expo)

```bash
cd Frontend
npm install
```

Create the `.env` file described below, then start the development server:

```bash
npx expo start
```

Then:

- Scan the QR code with **Expo Go** (Android) or the **Camera** app (iOS), **or**
- Press `a` for an Android emulator, `i` for an iOS simulator, `w` for web.

> **Note:** When testing on a physical device, `localhost` will not resolve to your development machine. Point the API base URL at your machine's LAN IP (e.g. `http://192.168.1.20:5000/api`) and make sure that address is included in the `AllowReactApp` CORS policy in `Program.cs`.

---

## ⚠️ Security Notice — Required Manual Configuration

> **The following files are intentionally excluded from version control (`.gitignore`) because they contain credentials, API keys, and private signing material. They are NOT present in this repository and MUST be created manually on every machine before the project will run.**
>
> **Never commit these files. Never paste their contents into issues, pull requests, documentation, or chat.**

### 4.1 `Backend/appsettings.Development.json` — **create manually**

```jsonc
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=YOUR_DB;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "YOUR_LONG_RANDOM_SECRET_AT_LEAST_32_CHARACTERS",
    "Issuer": "https://localhost:7001",
    "Audience": "https://localhost:7001"
  },
  "Gemini": {
    "ApiKey": "YOUR_GEMINI_API_KEY"
  },
  "AI": {
    "GeminiApiKey": "YOUR_GEMINI_API_KEY",
    "HuggingFaceToken": "YOUR_HUGGINGFACE_TOKEN"
  },
  "Firebase": {
    "ServiceAccountPath": "firebase-service-account.json"
  }
}
```

**Notes**

- `Jwt:Key` must be **at least 32 characters** — it is used as the symmetric HMAC signing key. Generate a fresh random value; never reuse a key across environments.
- `Jwt:Issuer` and `Jwt:Audience` must match the values validated in `Program.cs`.
- `Gemini:ApiKey` powers the chatbot and sentiment analysis; `AI:GeminiApiKey` powers the admin AI proxy. They may be the same key or two separate keys with independent quotas.
- For a production deployment, prefer **environment variables**, **.NET User Secrets** (`dotnet user-secrets set "Jwt:Key" "..."`), or a managed secret store over a JSON file on disk.

### 4.2 `Backend/firebase-service-account.json` — **create manually**

1. Open the [Firebase Console](https://console.firebase.google.com) → your project.
2. Go to **Project Settings → Service accounts → Generate new private key**.
3. Save the downloaded JSON as `firebase-service-account.json` in the API project root.
4. Ensure the path matches `Firebase:ServiceAccountPath` in your configuration.

> This file contains a **private RSA key** granting administrative access to your entire Firebase project. Treat it exactly as you would a password. If it is ever exposed, revoke the key immediately from the Firebase Console.

### 4.3 `Frontend/.env` — **create manually**

```bash
EXPO_PUBLIC_API_BASE_URL=https://localhost:7001/api
EXPO_PUBLIC_HUB_URL=https://localhost:7001/hubs/location

# Firebase Web SDK configuration
EXPO_PUBLIC_FIREBASE_API_KEY=your_firebase_web_api_key
EXPO_PUBLIC_FIREBASE_AUTH_DOMAIN=your-project.firebaseapp.com
EXPO_PUBLIC_FIREBASE_PROJECT_ID=your-project-id
EXPO_PUBLIC_FIREBASE_STORAGE_BUCKET=your-project.appspot.com
EXPO_PUBLIC_FIREBASE_MESSAGING_SENDER_ID=your_sender_id
EXPO_PUBLIC_FIREBASE_APP_ID=your_app_id
```

> Variables prefixed with `EXPO_PUBLIC_` are **embedded into the client bundle** and are readable by anyone who installs the app. Put only non-sensitive, client-safe values here. Server secrets (database credentials, Gemini keys, HuggingFace tokens, the Firebase service account) must **never** appear in the frontend.

### 4.4 Confirm `.gitignore` Coverage

Before your first commit, verify that the following patterns are ignored in both projects:

```gitignore
# Secrets & local configuration — never commit
.env
.env.*
appsettings.Development.json
appsettings.*.Local.json
firebase-service-account.json
*.pem
*.key
*.p12
*.p8
*.jks
secrets.json
```

If any of these files were ever committed, removing them in a later commit is **not sufficient** — they remain in Git history. In that case you must **rotate every affected credential** (database password, JWT signing key, Gemini key, HuggingFace token, Firebase service account) and purge the history with `git filter-repo` or BFG Repo-Cleaner.

---

## API Overview

All endpoints are served under `/api` and, unless noted, require an `Authorization: Bearer <token>` header. Endpoints in `AdminController` and `AiProxyController` additionally require the `Admin` role.

| Controller | Base Route | Responsibility |
|---|---|---|
| `AuthController` | `/api/Auth` | Registration, login, JWT issuance |
| `UsersController` | `/api/Users` | Profile, preferences, `GET /similar/{userId}` |
| `TripsController` | `/api/Trips` | Trip catalog, details, registration, admin edits |
| `CommunityController` | `/api/Community` | Feed, posts, likes, comments, reports |
| `ConnectionsController` | `/api/Connections` | Requests, `GET /status/{otherId}` |
| `MessagesController` | `/api/Messages` | 1-to-1 conversations |
| `ChatController` | `/api/Chat` | AI chatbot sessions and history |
| `FeedbacksController` | `/api/Feedbacks` | Feedback submission and sentiment |
| `NotificationsController` | `/api/Notifications` | Push dispatch, in-app inbox, read state |
| `MapController` | `/api/Map` | Routes, waypoints, active user locations |
| `FilesController` | `/api/Files` | Image upload → `/Uploads` |
| `AdminController` | `/api/Admin` | `stats/kpis`, `stats/attendance`, `stats/sentiment`, `stats/bot-efficiency`, `users`, `users/toggle-block/{id}`, `reports`, `posts/toggle-visibility/{id}` |
| `AiProxyController` | `/api/AiProxy` | `ping`, `push/generate`, `reports/summarize`, `toxicity/score` |
| **SignalR Hub** | `/hubs/location` | `UpdateLocation`, `SendInteractionRequest`, `SendChatMessage`, `LeaveChannel`, `BlockUser` |

Full request/response schemas are documented in **Swagger UI** at `/swagger`.

---

## License

Developed as an academic graduation project in collaboration with the **Society for the Protection of Nature in Israel (החברה להגנת הטבע)**. All rights reserved by the project authors and the partner organization.
