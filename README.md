🗣️ BadeHava — Real-World Chat Simulation (ASP.NET Web API)

“Bade Hava” (باد هوا) in Farsi literally means “air talk” or “wind talk” —
it refers to speaking into the air, like words that drift away and disappear.

This perfectly matches the project’s concept: conversations that exist only in the moment —
once spoken, they’re gone.

BadeHava is a unique chat backend built with .NET Web API that simulates real-world communication.
Unlike typical chat applications, BadeHava doesn’t store your messages anywhere — not in a database, not in memory, nowhere.
It only transfers messages in real-time between online users, creating a natural, real-world chatting experience.


🌍 Concept

In real life, when you talk to someone:

You both must be present to communicate.

You can’t send messages when the other person is offline or asleep.

Conversations don’t persist once they’re over — they exist only in the moment.

BadeHava brings that exact philosophy to online chatting.
If both users are online, they can exchange messages in real-time.
When one user goes offline, the connection ends — no history, no message saving, just pure real-time talk.


🧠 Tech Stack

.NET 9 Web API — core backend framework

dotnet ef (Entity Framework) — for DB handling

SignalR — for real-time message transfer
