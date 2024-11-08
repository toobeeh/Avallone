### Avallone
Avallone is the SignalR real-time communication backend for the skribbltypo extension.

> Following the tradition of naming the event-based backend for the typo extension after Palantir stones 
> (previous repos: Orthanc (php), Ithil (socket.io/js) and Ithil-Rebirth (socket.io/ts)), this repository shall be 
> named Avallone - after the master stone of all Palantiri.

## Core tasks
The core tasks define the signalr hub separation:
- guild lobbies: subscribing to guilds, listening for lobbies
- drops: receive and claim drops
- lobby: send lobby status, receive and send award events, show and claim drops

### Guild Lobbies Hub
Guild lobbies provide an interface for clients to subscribe to lobbies of online players that are connected to a guild.  
When subscribed to a guild, the client will receive updates for the currently online people of that guild.  
To subscribe, the client needs to authorize and have connected to that guild.  
On subscription, the client immediately receives the current lobbies.

### Online Items Hub
A simple interface which broadcasts the current online items to all clients.  
Freshly connected clients receive all current online items.  
Updates are only sent when a new item is added or removed.

### Drops Hub
Todo specify

### Lobby Hub
The lobby hub is an interface for subscribing to events of a specific skribbl lobby, and more importantly, reporting the current lobby status to the server.

#### Server-side lobby identification
The server persists typo-specific lobby settings in the database.  
Lobbies are keyed with their skribbl invite.  
Invites are unique during a skribbl server lifecycle, but may be repeated after a restart.  
For this purpose, next to their ID, a timestamp for discovered shall be saved. Lobbies whose discovered timestamp exceed a certain reasonable time like two days shall be treated as outdated.

Lobbies have a typo ownership assigned.
A lobby owner is authorized to set properties like lobby privacy and description.  
Lobby ownership is proved with using a signed timestamp.
When a client discovers a lobby, it receives a token which represents their owner heritage order. 
When the owner disconnects, all other connected clients are notified and can claim ownership by sending their ownership token.
The client with the oldest claim timestamp will be selected as the new lobby owner.  
All clients receive the lobby settings which includes the claim (timestamp) of the current owner to identify if their claim was successful.  
However, since ownership can only be claimed with the signed claim, the claim itself is no sensitive information.  
When a client reconnects - this is also necessary to maintain heritage order during server restarts -, it can use its previous signed token to reclaim their lobby ownership, even if in the meantime another client with a lesser prioritized claim has claimed ownership.  
This mechanism needs no persistence of data except a secret key which the server uses to sign the ownership tokens, and the currently selected claim of a lobby.

#### Client lobby assignment
After a client connects to the lobby hub, it shall send a "discovered" event, where the server is prompted to check whether a lobby with that ID already exists, and return default data if new or the existing lobby data.  
The client connection id will be assigned to that lobby using a LobbyContext and can interact with the hub from now on.  
On subsequent hub calls, the client will be identified by fetching the LobbyContext via their connection id.
If the ownership token is valid and precedes the current lobby owner, the lobby ownership is passed to the client.

Clients can report their lobby status by sending lobby data to a hub method. The hub will save this data with a timestamp. 
Typo lobbies contain of the TypoState and the SkribblState.  
The skribbl state contains all skribbl-relevant details, while the typo state contain management relevant details like ownership and privacy.  
Both states are stored in the same database entity, but managed separately via valmar.

When the typo  settings are updated, all clients in that lobby shall receive an update with the new settings.  
Clients never need to be notified by skribbl state updates.

### Client lobby disconnection
When a client leaves a lobby, it shall disconnect from the hub.  
It will no longer receive any updates from the lobby and its LobbyContext will be removed.  
The server checks if the client was the lobby owner and removes its claim from the lobby data.
Other clients in the lobby group receive an event so that they may send their ownership claim.
