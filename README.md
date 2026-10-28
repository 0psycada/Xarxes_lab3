Martí Mach – Deliverable 3

GitHub: https://github.com/0psycada/Xarxes_lab3 
Versió de Unity: 6000.4.9f1

es un topdown shooter en linea: cada jugador és una càpsula, es mou, salta, s'ajup, i dispares apuntant amb el ratolí. El primer que arriba a 5 kills guanya

## Execució:

1. Obrir `lab3xarxes.exe` a dos PCs 
2. un fa de Host i posa un nom, tria el protocol del lobby (TCP o UDP), Create game → Create. El host també juga.
3. els altres jugadors posen un nom diferent, el mateix protocol que el host i li donen a jjjoin game, la IP del host i el port (per defecte `9050`) → Join.
4. quan esta tothom li dones a start game i comença el joc
Ports: el lobby fa servir el port triat (9050) i el joc fa servir UDP al port + 1 (9051)

Controls: 
**WASD** moviment
**Espai** saltar
**C/Ctrl** ajupir-se
**ratolí** apuntar
**clic esquerre** disparar

## Estat o input

Els clients envien input i el servidor envia estat

Això ho faig perque el servidor és l'únic que decideixi on és cadascú, qui ha encertat qui i qui guanya. 
Si cada client envia la seva posició i els seus impactes, dos PCs podrien dir coses diferents i es podrien fer trampes. 
Amb input, el client només pot dir quines tecles prem. aixo es a costa de que el moviment propi es vegi amb un retard, en una LAN són pocs mil·lisegons.

El host també és un client del seu propi servidor i ha de passar pel mateix camí que els altres

## Protocol

Tots els missatges comencen amb un byte `MsgType`:

Les dades del joc van en binari amb el `Serializer` (`BinaryWriter`/`BinaryReader`, floats de 4 bytes i mai com a text). El text només es fa servir al lobby i al xat.

## Extres fets

- Moviment per xarxa
- Disparar per hitscan resolt al servidor
- si t'ajups el hitscan no et dona
- mes de 2 jugadors (en teoria)
- un cicle de partida completa que pots guanyar o perdre

## Bugs coneguts

- Si el host tanca amb el lobby en UDP, els altres tornen al menú als 5 s (el *timeout*), no a l'instant. En TCP és immediat.
- Si el tallafocs bloqueja el port UDP del joc (port + 1), el client es queda a "Waiting for the game server…" sense cap altre avís.
- El port del joc no autentica els jugadors: qui conegui un nom de la sala podria enviar `GameJoin` amb aquest nom i prendre-li el control.
- L'estat s'envia encara que no hagi canviat res.

## Què ha canviat des de l'entrega 2

Com no vaig ser capaç d'entregar l'entrega 2 i cada persona del grup ha decidit anar pel seu compte he fet aquesta entrega a partir del lobby de la solució del Lab 3 que es va donar a classe 
si que he canviat:
- `Scripts/Game` (`GameServer`, `GameClient`, `UdpChannel`, `GameView`, `PlayerAvatar`, `PlayerInput`, `GameManager`), `UI/GameUI` i afegit `Player` i `Tracer`.
- Nous DTOs (`InputDTO`, `StateDTO`, `PlayerStateDTO`, `GamePhase`) i nous `MsgType` (3, 4, 5); el `Serializer` els escriu i llegeix.
- el script de`Messages` ara construeix i llegeix tots els tipus i`ReadLobby` rebutja els missatges de joc.
- el LobbyServer.cs repeteix `START:` per cada pong i `LobbyClient` només canvia d'escena el primer cop.


