<div align="center">

<img src="./assets/images/icon.png" alt="PartnersApp Logo" width="130" style="border-radius: 50%; border: 3px solid #2d6a4f;" />

# 🥾 PartnersApp — שותפים לדרך

### **Never hike alone again.**

A social hiking platform built for the **Society for the Protection of Nature in Israel**
*(החברה להגנת הטבע)* — connecting hikers, powered by real-time location and AI.

<br/>

[![React Native](https://img.shields.io/badge/React_Native-0.81-61DAFB?style=for-the-badge&logo=react&logoColor=black)](https://reactnative.dev)
[![Expo](https://img.shields.io/badge/Expo_SDK-54-000020?style=for-the-badge&logo=expo&logoColor=white)](https://expo.dev)
[![.NET](https://img.shields.io/badge/ASP.NET_Core-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com)
[![SQL Server](https://img.shields.io/badge/SQL_Server-2019+-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/sql-server)

[![SignalR](https://img.shields.io/badge/SignalR-Realtime-0078D4?style=flat-square&logo=microsoft&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet/signalr)
[![Gemini](https://img.shields.io/badge/Google_Gemini-AI-8E75B2?style=flat-square&logo=googlegemini&logoColor=white)](https://ai.google.dev)
[![Hugging Face](https://img.shields.io/badge/HuggingFace-Moderation-FFD21E?style=flat-square&logo=huggingface&logoColor=black)](https://huggingface.co)
[![Firebase](https://img.shields.io/badge/Firebase-FCM-FFCA28?style=flat-square&logo=firebase&logoColor=black)](https://firebase.google.com)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.9-3178C6?style=flat-square&logo=typescript&logoColor=white)](https://www.typescriptlang.org)
[![Platform](https://img.shields.io/badge/Platform-iOS_|_Android_|_Web-lightgrey?style=flat-square)]()
[![RTL](https://img.shields.io/badge/UI-Hebrew_RTL-1e8425?style=flat-square)]()

<br/>

### 🎬 **[Watch the Demo](https://www.youtube.com/watch?v=xWfUXMGiqBE)** &nbsp;·&nbsp; 📄 **[View the Poster](./docs/poster.pdf)**

</div>

---

<div align="center">

## 🎥 Video Demo

<a href="https://www.youtube.com/watch?v=xWfUXMGiqBE" target="_blank">
  <img src="https://img.youtube.com/vi/xWfUXMGiqBE/maxresdefault.jpg" alt="PartnersApp — Video Demo" width="720" />
</a>

**▶️ [Click to watch the full walkthrough on YouTube](https://www.youtube.com/watch?v=xWfUXMGiqBE)**

*A complete tour: registration → smart matching → live proximity map → community feed → AI chatbot → admin dashboard.*

<br/>

## 📄 Project Poster

[![Download Poster](https://img.shields.io/badge/📄_Project_Poster-View_or_Download_PDF-EA4335?style=for-the-badge)](./docs/poster.pdf)

The academic poster summarizing the problem, solution, architecture, and results
is available at [`./docs/poster.pdf`](./docs/poster.pdf).

</div>

---

## 📑 Table of Contents

| | | |
|---|---|---|
| [🌿 About The Project](#-about-the-project) | [✨ Key Features](#-key-features) | [🛠️ Tech Stack](#️-tech-stack) |
| [🏗️ Project Architecture](#️-project-architecture) | [🚀 Getting Started](#-getting-started--installation) | [🔐 Security Notice](#-security-notice--required-manual-configuration) |
| [🔌 API Overview](#-api-overview) | [👥 Authors](#-authors) | [📄 License](#-license) |

---

## 🌿 About The Project

**PartnersApp ("שותפים לדרך")** is a cross-platform mobile application built for the *Society for the Protection of Nature in Israel*. It addresses a concrete problem the organization faces: people want to join guided nature trips, but many hesitate to sign up alone, drop out before the trip date, or never connect with the community that forms around these hikes.

> **The idea in one line:** turn a one-off trip registration into an ongoing social experience — before, during, and after the hike.

<table>
<tr>
<td width="33%" valign="top">

### 🔎 Before the trip
Browse the organization's catalog of nature trips by category, location, date, and difficulty — then register directly from the phone. A **two-layer matching engine** suggests hiking partners and explains *why* they match.

</td>
<td width="33%" valign="top">

### 📍 During the trip
A **real-time proximity map** (SignalR) surfaces consenting hikers within a **1 km radius**. An explicit consent flow — request → accept → private ephemeral channel — is enforced server-side with rate limiting and blocking.

</td>
<td width="33%" valign="top">

### 💬 After the trip
A **community feed** with photos, likes, and comments. Feedback is scored by **AI sentiment analysis**, and the organization's staff sees it all in a live **admin dashboard**.

</td>
</tr>
</table>

### 🎯 Project Goals

- **Reduce solo-hiker drop-off** by matching participants with compatible partners before departure.
- **Build a persistent community** around the organization's trips rather than isolated events.
- **Give staff actionable insight** — RSVP-vs-attendance analytics, sentiment trends, and moderation tooling in one place.
- **Keep safety and consent first-class** — no location is shared without an explicit opt-in, and every interaction is gated, rate-limited, and revocable.

> [!NOTE]
> The entire user-facing product is **Hebrew-first and fully RTL**, matching the organization's audience.

---

## ✨ Key Features

<table>
<tr><th width="22%">Area</th><th>Capability</th></tr>
<tr><td>🔐 <b>Authentication</b></td><td>JWT-based registration &amp; login, role separation (<code>User</code> / <code>Admin</code>), account blocking</td></tr>
<tr><td>🥾 <b>Trips</b></td><td>Browse, filter, register, personal trip history, full admin CRUD</td></tr>
<tr><td>🧠 <b>Smart Matching</b></td><td>Two-layer scoring — <b>profile similarity</b> (shared interests, city, family status) + <b>behavioral taste</b> derived from trip ratings — returned with a 0–100 score and human-readable reasons</td></tr>
<tr><td>🤝 <b>Connections</b></td><td>Friend-request flow (<code>none</code> → <code>pending</code> → <code>accepted</code> / <code>rejected</code>) with a personal intro message</td></tr>
<tr><td>📍 <b>Live Proximity Map</b></td><td>SignalR hub broadcasting positions within a 1 km <b>Haversine</b> radius · consent-gated private channels · blocking · per-user rate limiting (5 req/min)</td></tr>
<tr><td>💬 <b>Real-Time Chat</b></td><td>Persistent 1-to-1 messaging plus ephemeral in-session channels over SignalR</td></tr>
<tr><td>🖼️ <b>Community Feed</b></td><td>Posts with multi-image upload, likes, comments, and content reporting</td></tr>
<tr><td>🤖 <b>AI Chatbot</b></td><td>Gemini-powered Hebrew assistant ("פארטנר") with the user's own trip data injected as context, plus a multi-model fallback chain</td></tr>
<tr><td>😊 <b>Sentiment Analysis</b></td><td>Feedback classified as <code>Positive</code> / <code>Negative</code> / <code>Neutral</code> / <code>Urgent_Negative</code> with a 0–100 score and a short Hebrew summary</td></tr>
<tr><td>🛡️ <b>Toxicity Scoring</b></td><td>HuggingFace <code>unitary/toxic-bert</code> scoring for moderation triage (<code>low</code> / <code>medium</code> / <code>high</code>)</td></tr>
<tr><td>📣 <b>Smart Push Generator</b></td><td>Gemini writes Hebrew push copy per campaign goal — signup, reminder, equipment, hype, feedback</td></tr>
<tr><td>🔔 <b>Push Notifications</b></td><td>Firebase Cloud Messaging via Firebase Admin SDK + Expo Notifications</td></tr>
<tr><td>📊 <b>Admin Dashboard</b></td><td>KPIs · attendance analytics · sentiment charts · bot efficiency · grouped reports · user risk scoring · one-tap moderation</td></tr>
<tr><td>🗺️ <b>Route Planning</b></td><td>Saveable routes with ordered waypoints, computed distance, and a shareable token</td></tr>
</table>

---

## 🛠️ Tech Stack

### 📱 Frontend — React Native / Expo

| Technology | Version | Purpose |
|---|---|---|
| **React Native** + **React** | `0.81` / `19` | Core UI framework |
| **Expo SDK** *(New Architecture)* | `54` | Build tooling, native modules, OTA-ready pipeline |
| **Expo Router** | `6` | File-based routing with typed routes |
| **TypeScript** | `5.9` | Type safety across a mixed `.tsx` / `.jsx` codebase |
| **@microsoft/signalr** | `10` | Real-time client for location, presence, and chat |
| **react-native-maps** + **map-clustering** | `1.20` | Map rendering and marker clustering |
| **expo-location** | `19` | Foreground GPS tracking |
| **expo-notifications** | `0.32` | Push registration &amp; handling |
| **firebase** *(Web SDK)* | `12` | Client-side Firebase integration |
| **expo-image-picker** / **expo-image** | `17` / `3` | Image selection and optimized rendering |
| **react-native-reanimated** + **worklets** | `4` | Animations |
| **lottie-react-native** | `7.3` | Splash and loading animations |
| **@expo-google-fonts/rubik** | — | Hebrew-friendly typography |
| **async-storage** | `2.2` | JWT and session persistence |
| **expo-speech** | `14` | Text-to-speech for accessibility |
| **ESLint** *(eslint-config-expo)* | `9` | Linting |

### ⚙️ Backend — ASP.NET Core Web API

| Technology | Purpose |
|---|---|
| **ASP.NET Core Web API** (`PartnersWebApi`) | REST API host |
| **SignalR** | `LocationHub` at `/hubs/location` — live location, presence, private channels |
| **JWT Bearer Authentication** | Token issuance &amp; validation, including SignalR query-string tokens |
| **Swagger / Swashbuckle** | Interactive API documentation with Bearer auth |
| **Dapper** + **System.Data.SqlClient** | Data access — Dapper for map/route queries, ADO.NET for stored procedures |
| **IHttpClientFactory** | Named, timeout-bounded clients for `gemini` (30s) and `huggingface` (20s) |
| **Hosted Service** — `StaleSessionCleaner` | Background cleanup of stale presence sessions |
| **Singleton** — `PresenceStore` | In-memory presence, blocking, channels, and rate limiting |
| **Static File Middleware** | Serving user-uploaded images from `/Uploads` |
| **CORS Policy** — `AllowReactApp` | Explicit origin allow-list with credentials |

### 🗄️ Database — Microsoft SQL Server

All core business logic is executed through **stored procedures** (`SP_*_Nature`) — registration, trips, feedback, community, notifications, chat, admin KPIs, and matching (`SP_GetMatchingData_Nature`).

<details>
<summary><b>📋 Core tables</b></summary>

<br/>

`Users` · `Trips` · `UserTrips` · `Feedbacks` · `Posts` · `Comments` · `Reports` · `Notifications` · `ChatSessions` · `Messages` · `Connections` · `Routes` · `Waypoints` · `LiveLocations`

</details>

### 🤖 AI Services

| Service | Model(s) | Used For |
|---|---|---|
| **Google Gemini API** | `gemini-2.5-flash` · `gemini-2.0-flash` · `gemini-2.0-flash-lite` · `gemini-2.5-flash-lite` · `gemini-3-flash-preview` | Hebrew chatbot, sentiment analysis, report summarization, smart push generation |
| **HuggingFace Inference API** | `unitary/toxic-bert` | Toxicity scoring for community moderation |

> [!TIP]
> Both AI integrations implement an **automatic fallback chain** — on `429` (quota exhausted), `503` (unavailable), or `404`, the next model in the list is tried automatically. A free-tier quota limit therefore degrades gracefully instead of breaking the feature. A diagnostic endpoint (`GET /api/AiProxy/ping`) reports the live health of every model in the chain.

### 🔔 Notifications

| Technology | Purpose |
|---|---|
| **Firebase Admin SDK** (`FirebaseAdmin`, `Google.Apis.Auth`) | Server-side push dispatch |
| **Firebase Cloud Messaging (FCM)** | Delivery channel |
| **Expo Notifications** | Device token registration and client-side display |

---

## 🏗️ Project Architecture

The repository is split into two independent applications: a **React Native client** and an **ASP.NET Core API**.

### 🔄 Request Flow

```
                     React Native (Expo)
                             │
                  HTTPS  +  Bearer JWT
                             ▼
        ┌────────────────────────────────────────────┐
        │        ASP.NET Core Controllers            │
        └────────────────────┬───────────────────────┘
                             │
        ┌────────────────────┼────────────────────────────────┐
        ▼                    ▼                                ▼
 Repository Interfaces   AI Services                  SignalR LocationHub
        │                    │                                │
        ▼                    ├──► Google Gemini API      WebSocket ◄──► Client
  SQL Server                 └──► HuggingFace API
 (Stored Procs)
        │                Firebase Admin SDK ──► FCM ──► Device
        └──► Dapper (routes / live locations)
```

<details open>
<summary><h3>📱 Frontend Structure</h3></summary>

```
Frontend/                           # React Native + Expo client
├── app/                            # Expo Router — file-based routing
│   ├── _layout.jsx                 # Root layout, providers, splash
│   ├── index.jsx                   # Entry / redirect
│   ├── login.jsx  ·  register.jsx  # Authentication
│   ├── (tabs)/                     # Main tab navigator
│   │   ├── _layout.jsx
│   │   ├── hub.jsx                 # Home hub
│   │   ├── mytrips.jsx             # User's trips
│   │   ├── community.jsx           # Social feed
│   │   ├── chat.jsx                # AI chatbot
│   │   ├── profile.jsx
│   │   └── dashboard.jsx           # Admin-only dashboard
│   ├── trips/[tripId].jsx          # Dynamic trip details
│   ├── chats/[otherId].jsx         # 1-to-1 conversation
│   ├── similar.jsx                 # Smart matching results
│   ├── requests.jsx                # Connection requests
│   ├── proximity.jsx               # Live proximity screen
│   └── map.jsx                     # Routes & waypoints map
│
├── components/                     # Reusable UI
│   ├── AppHeader.jsx  ·  TripCard.jsx  ·  PostCard.jsx
│   ├── ProximityMap.tsx  ·  Chatpanel.tsx
│   ├── RecommendationModal.jsx
│   ├── LoadingScreen.jsx  ·  AnimatedSplash.jsx
│   └── themed-text.tsx  ·  themed-view.tsx  ·  …
│
├── hooks/                          # Custom logic hooks
│   ├── useProximityHub.ts          # SignalR connection lifecycle
│   ├── useLocation.ts              # GPS subscription
│   ├── useSmartPush.js             # AI push generation
│   ├── useReportSummary.js         # AI report summarization
│   ├── useReportSeverity.js        # Report severity heuristics
│   ├── useUserRiskScoring.js       # Admin risk scoring
│   └── use-color-scheme.ts  ·  use-theme-color.ts
│
├── assets/                         # Images, icons, Lottie animations
├── app.json  ·  package.json  ·  tsconfig.json
└── .env                            # ⚠️ NOT in the repository — create manually
```

</details>

<details open>
<summary><h3>⚙️ Backend Structure</h3></summary>

```
Backend/                            # PartnersWebApi — ASP.NET Core
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
│   ├── AiProxyController.cs        # Admin-only AI proxy
│   └── AdminController.cs          # Dashboard KPIs & moderation
│
├── Hubs/
│   └── LocationHub.cs              # SignalR: presence, proximity, channels, blocking
│
├── Interfaces/                     # Repository & service contracts
│   ├── IUsersRepository.cs  ·  ITripsRepository.cs  ·  ICommunityRepository.cs
│   ├── IChatRepository.cs  ·  IMessagesRepository.cs  ·  IConnectionsRepository.cs
│   ├── INotificationsRepository.cs  ·  IFeedbacksRepository.cs
│   ├── IDashboardRepository.cs
│   └── IMapService.cs
│
├── Repositories/                   # SQL Server implementations
│   ├── SQLUsersRepository.cs       # incl. the two-layer matching algorithm
│   ├── SQLTripsRepository.cs  ·  SQLCommunityRepository.cs
│   ├── SQLChatRepository.cs  ·  SQLMessagesRepository.cs
│   ├── SQLConnectionsRepository.cs  ·  SQLNotificationsRepository.cs
│   ├── SQLFeedbacksRepository.cs  ·  DashboardRepository.cs
│   └── MapService.cs
│
├── Services/
│   ├── GeminiAiService.cs          # Chat + sentiment (IChatAiService)
│   ├── PresenceStore.cs            # In-memory presence / blocks / channels
│   └── StaleSessionCleaner.cs      # Background cleanup
│
├── Models/                         # Entities & DTOs
│   ├── User.cs  ·  Trip.cs  ·  UserTrip.cs  ·  Feedback.cs  ·  Community.cs
│   ├── Chatbot.cs  ·  Notification.cs  ·  Route.cs  ·  Waypoint.cs  ·  LiveLocation.cs
│   └── SimilarUserDto.cs  ·  RouteDto.cs  ·  ChatDtos.cs  ·  ConnectionDtos.cs
│
├── Uploads/                        # Runtime-generated image storage
├── Program.cs                      # DI, JWT, CORS, SignalR, Firebase, pipeline
├── appsettings.json                # ⚠️ Non-secret defaults only
├── appsettings.Development.json    # ⚠️ NOT in the repository — create manually
└── firebase-service-account.json   # ⚠️ NOT in the repository — create manually
```

</details>

---

## 🚀 Getting Started & Installation

### 📋 Prerequisites

| Requirement | Version / Source |
|---|---|
| ![.NET](https://img.shields.io/badge/.NET_SDK-8.0+-512BD4?style=flat-square&logo=dotnet&logoColor=white) | `8.0` or later |
| ![Node](https://img.shields.io/badge/Node.js-20.x_LTS-339933?style=flat-square&logo=nodedotjs&logoColor=white) | `20.x` LTS or later (npm `10.x`+) |
| ![SQL Server](https://img.shields.io/badge/SQL_Server-2019+-CC2927?style=flat-square&logo=microsoftsqlserver&logoColor=white) | Local or reachable remote instance |
| ![VS](https://img.shields.io/badge/IDE-VS_2022_/_VS_Code-5C2D91?style=flat-square&logo=visualstudio&logoColor=white) | With ASP.NET &amp; C# workloads |
| ![Expo Go](https://img.shields.io/badge/Expo_Go-Device_or_Emulator-000020?style=flat-square&logo=expo&logoColor=white) | Physical iOS/Android device or emulator |
| 🔑 **Gemini API key** | [aistudio.google.com](https://aistudio.google.com) |
| 🔑 **HuggingFace token** | [huggingface.co/settings/tokens](https://huggingface.co/settings/tokens) |
| 🔑 **Firebase project** | With a generated service-account key |

---

### 1️⃣ Clone the Repository

```bash
git clone <your-repository-url>
cd PartnersApp
```

---

### 2️⃣ Database Setup

```sql
-- 1. Create the database
CREATE DATABASE PartnersAppDb;
GO
```

```bash
# 2. Execute the schema and stored-procedure scripts against it
#    SQL.txt  +  all SP_*_Nature.sql files
```

> [!IMPORTANT]
> The API depends on the stored procedures **exclusively** — there is no code-first migration fallback. Verify that every `SP_*_Nature` procedure was created before starting the backend.

---

### 3️⃣ Backend Setup — `PartnersWebApi`

```bash
cd Backend
dotnet restore
```

**➡️ Now create the local configuration files** described in the [Security Notice](#-security-notice--required-manual-configuration) below, then run:

```bash
dotnet run
```

| Endpoint | URL |
|---|---|
| 🌐 **API** | `https://localhost:7xxx/api` |
| 📘 **Swagger UI** | `https://localhost:7xxx/swagger` |
| 🔌 **SignalR Hub** | `https://localhost:7xxx/hubs/location` |

*(Exact port is defined in `Properties/launchSettings.json`.)*

---

### 4️⃣ Frontend Setup — Expo

```bash
cd Frontend
npm install
```

**➡️ Create the `.env` file** described below, then start the dev server:

```bash
npx expo start
```

Then choose your target:

```bash
# Scan the QR code with Expo Go (Android) or the Camera app (iOS)
#   or press:
a   # → Android emulator
i   # → iOS simulator
w   # → Web browser
```

> [!WARNING]
> **Testing on a physical device?** `localhost` will not resolve to your development machine. Point the API base URL at your machine's LAN IP (e.g. `http://192.168.1.20:5000/api`) and add that origin to the `AllowReactApp` CORS policy in `Program.cs`.

---

## 🔐 Security Notice — Required Manual Configuration

> [!CAUTION]
> **The following files are intentionally excluded from version control (`.gitignore`) because they contain credentials, API keys, and private signing material.**
>
> **They are NOT present in this repository and MUST be created manually on every machine before the project will run.**
>
> ❌ **Never commit these files.** ❌ **Never paste their contents into issues, pull requests, documentation, or chat.**

<div align="center">

| File | Location | Contains |
|---|---|---|
| 🔴 `appsettings.Development.json` | `Backend/` | DB connection string, JWT key, Gemini &amp; HuggingFace keys |
| 🔴 `firebase-service-account.json` | `Backend/` | Firebase private RSA key |
| 🟡 `.env` | `Frontend/` | Public client configuration (API base URL, Firebase Web SDK) |

</div>

---

### 🔴 4.1 `Backend/appsettings.Development.json`

<details open>
<summary><b>Click to expand the template</b></summary>

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

</details>

**Notes**

- `Jwt:Key` must be **at least 32 characters** — it is the symmetric HMAC signing key. Generate a fresh random value; never reuse a key across environments.
- `Jwt:Issuer` and `Jwt:Audience` must match the values validated in `Program.cs`.
- `Gemini:ApiKey` powers the chatbot and sentiment analysis; `AI:GeminiApiKey` powers the admin AI proxy. They may be the same key, or two separate keys with independent quotas.
- For production, prefer **environment variables**, **.NET User Secrets**, or a managed secret store over a JSON file on disk:

```bash
dotnet user-secrets set "Jwt:Key" "your-long-random-secret"
dotnet user-secrets set "AI:GeminiApiKey" "your-gemini-key"
```

---

### 🔴 4.2 `Backend/firebase-service-account.json`

```
1.  Firebase Console  →  your project
2.  Project Settings  →  Service accounts  →  Generate new private key
3.  Save the downloaded JSON as  firebase-service-account.json  in the API project root
4.  Ensure the path matches  Firebase:ServiceAccountPath  in your configuration
```

> [!CAUTION]
> This file contains a **private RSA key** granting administrative access to your entire Firebase project. Treat it exactly as you would a root password. If it is ever exposed, **revoke the key immediately** from the Firebase Console.

---

### 🟡 4.3 `Frontend/.env`

<details open>
<summary><b>Click to expand the template</b></summary>

```bash
# ── API ────────────────────────────────────────────────
EXPO_PUBLIC_API_BASE_URL=https://localhost:7001/api
EXPO_PUBLIC_HUB_URL=https://localhost:7001/hubs/location

# ── Firebase Web SDK ───────────────────────────────────
EXPO_PUBLIC_FIREBASE_API_KEY=your_firebase_web_api_key
EXPO_PUBLIC_FIREBASE_AUTH_DOMAIN=your-project.firebaseapp.com
EXPO_PUBLIC_FIREBASE_PROJECT_ID=your-project-id
EXPO_PUBLIC_FIREBASE_STORAGE_BUCKET=your-project.appspot.com
EXPO_PUBLIC_FIREBASE_MESSAGING_SENDER_ID=your_sender_id
EXPO_PUBLIC_FIREBASE_APP_ID=your_app_id
```

</details>

> [!WARNING]
> Variables prefixed with `EXPO_PUBLIC_` are **embedded into the client bundle** and readable by anyone who installs the app. Put only non-sensitive, client-safe values here. Server secrets — database credentials, Gemini keys, HuggingFace tokens, the Firebase service account — must **never** appear in the frontend.

---

### 🛡️ 4.4 Confirm `.gitignore` Coverage

Before your first commit, verify that these patterns are ignored in **both** projects:

```gitignore
# ── Secrets & local configuration — never commit ──
.env
.env.*
appsettings.Development.json
appsettings.*.Local.json
firebase-service-account.json
secrets.json
*.pem
*.key
*.p12
*.p8
*.jks
```

> [!CAUTION]
> **If any of these files were ever committed, deleting them in a later commit is NOT sufficient** — they remain permanently in Git history.
>
> You must **rotate every affected credential** (database password, JWT signing key, Gemini key, HuggingFace token, Firebase service account) **and** purge the history with [`git filter-repo`](https://github.com/newren/git-filter-repo) or [BFG Repo-Cleaner](https://rtyley.github.io/bfg-repo-cleaner/).

---

## 🔌 API Overview

All endpoints are served under `/api`. Unless noted, every request requires an `Authorization: Bearer <token>` header. `AdminController` and `AiProxyController` additionally require the **`Admin`** role.

| Controller | Base Route | Responsibility |
|---|---|---|
| 🔐 `AuthController` | `/api/Auth` | Registration, login, JWT issuance |
| 👤 `UsersController` | `/api/Users` | Profile, preferences, `GET /similar/{userId}` |
| 🥾 `TripsController` | `/api/Trips` | Trip catalog, details, registration, admin edits |
| 🖼️ `CommunityController` | `/api/Community` | Feed, posts, likes, comments, reports |
| 🤝 `ConnectionsController` | `/api/Connections` | Requests, `GET /status/{otherId}` |
| 💬 `MessagesController` | `/api/Messages` | 1-to-1 conversations |
| 🤖 `ChatController` | `/api/Chat` | AI chatbot sessions and history |
| ⭐ `FeedbacksController` | `/api/Feedbacks` | Feedback submission and sentiment |
| 🔔 `NotificationsController` | `/api/Notifications` | Push dispatch, in-app inbox, read state |
| 🗺️ `MapController` | `/api/Map` | Routes, waypoints, active user locations |
| 📁 `FilesController` | `/api/Files` | Image upload → `/Uploads` |
| 📊 `AdminController` | `/api/Admin` | `stats/kpis` · `stats/attendance` · `stats/sentiment` · `stats/bot-efficiency` · `users` · `users/toggle-block/{id}` · `reports` · `posts/toggle-visibility/{id}` |
| 🧠 `AiProxyController` | `/api/AiProxy` | `ping` · `push/generate` · `reports/summarize` · `toxicity/score` |
| ⚡ **SignalR Hub** | `/hubs/location` | `UpdateLocation` · `SendInteractionRequest` · `SendChatMessage` · `LeaveChannel` · `BlockUser` |

> [!TIP]
> Full request/response schemas — including Bearer-token authorization — are documented interactively in **Swagger UI** at `/swagger`.

---

## 👥 Authors & Partnerships

<div align="center">

Developed as a final-year graduation project in collaboration with:

<br/>

<p align="center">
  <a href="https://www.teva.org.il/">
    <img src="./assets/images/logo.png" alt="SPNI Logo" width="160" height="70" style="object-fit: contain;" />
  </a>
  &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
  <a href="https://www.ruppin.ac.il/">
    <img src="./assets/images/Ruppinlogo.jpg" alt="Ruppin Logo" width="160" height="70" style="object-fit: contain;" />
  </a>
</p>

<br/>

[![Demo](https://img.shields.io/badge/▶_Watch_the_Demo-FF0000?style=for-the-badge&logo=youtube&logoColor=white)](https://www.youtube.com/watch?v=xWfUXMGiqBE)
[![Poster](https://img.shields.io/badge/📄_Project_Poster-EA4335?style=for-the-badge)](./docs/poster.pdf)

</div>

---

## 📄 License

All rights reserved by the project authors and the partner organization.
This project was developed for academic purposes in collaboration with the Society for the Protection of Nature in Israel.

<div align="center">

