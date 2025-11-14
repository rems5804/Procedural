# Procedural Generation (Vue rapide)

Petit récap des méthodes de génération dispo dans le projet. Objectif: aller droit au but pour retrouver l’essentiel.

## Structure de base
- `ProceduralGridGenerator` : composant qui crée la grille puis lance la méthode. Paramètres: Seed, StepDelay (ms), Debug.
- `ProceduralGenerationMethod` : ScriptableObject abstrait. Fournit `Generate()` + annulation via `CancellationToken`.
- Pipeline: `Initialize(generator, randomService)` puis `Generate()` qui appelle votre `ApplyGeneration(token)`.

Exemple du Code:
```csharp
public async UniTask Generate() {
    _cancellationTokenSource?.Cancel();
    await UniTask.Delay(GridGenerator.StepDelay + 100);
    _cancellationTokenSource = new CancellationTokenSource();
    await UniTask.SwitchToMainThread();
    await ApplyGeneration(_cancellationTokenSource.Token);
}
```

## 1. Simple Room Placement
Place un certain nombre de salles rectangulaires aléatoires sans chevauchement (petit buffer) puis relie chaque salle à la suivante par un couloir en "L".
- Paramètres principaux: MaxRooms, RoomMinSize, RoomMaxSize.
- Vérifie la place avec `CanPlaceRoom(room, 1)`.
- Couloirs: orientation aléatoire (horizontal d’abord ou vertical d’abord).

Boucle de placement:
```csharp
for (int i = 0; i < _maxSteps && roomsPlacedCount < _maxRooms; i++) {
    int w = RandomService.Range(_roomMinSize.x, _roomMaxSize.x + 1);
    int h = RandomService.Range(_roomMinSize.y, _roomMaxSize.y + 1);
    int x = RandomService.Range(0, Grid.Width - w);
    int y = RandomService.Range(0, Grid.Lenght - h);
    var room = new RectInt(x, y, w, h);
    if (!CanPlaceRoom(room, 1)) continue;
    PlaceRoom(room);
    roomsPlacedCount++;
    await UniTask.Delay(GridGenerator.StepDelay, cancellationToken: token);
}
```
Couloir en L:
```csharp
void CreateDogLegCorridor(Vector2Int a, Vector2Int b) {
    if (RandomService.Chance(0.5f)) { 
        CreateHorizontalCorridor(a.x, b.x, a.y);
        CreateVerticalCorridor(a.y, b.y, b.x);
    } else { 
        CreateVerticalCorridor(a.y, b.y, a.x);
        CreateHorizontalCorridor(a.x, b.x, b.y);
    }
}
```

## 2. Cellular Automata
Automate binaire sur la grille . Remplissage initial pseudo-aléatoire selon un pourcentage puis plusieurs itérations de "lissage" basées sur le nombre de voisins.
- Paramètres: randomFillPercent, iterations, groundThreshold.
- Stocke trois matrices: `_grid` (courant), `_buffer` (prochain état), `_applied` (ce qui a déjà été instancié).
- Optimise: applique uniquement les cellules qui changent.

Étape de transition:
```csharp
for (int y = 0; y < h; y++) {
  for (int x = 0; x < w; x++) {
    int neigh = CountGroundNeighbors(x,y,w,h);
    _buffer[x,y] = neigh >= groundThreshold;
  }
}
// swap
var tmp = _grid; _grid = _buffer; _buffer = tmp;
```
Comptage des voisins (8 directions):
```csharp
int CountGroundNeighbors(int x,int y,int w,int h){
  int c=0; for(int dy=-1;dy<=1;dy++){int ny=y+dy; if(ny<0||ny>=h)continue;
    for(int dx=-1;dx<=1;dx++){ if(dx==0&&dy==0)continue; int nx=x+dx; if(nx<0||nx>=w)continue; if(_grid[nx,ny]) c++; }
  } return c;
}
```

## 3. BSP 
Division récursive de la zone en sous-rectangles (noeuds). Chaque feuille pose une salle. Les feuilles sœurs sont reliées par couloirs.
- Paramètres: HorizontalSplitChance, SplitRatio(min/max), MaxSplitAttempt, LeafMinSize, RoomMin/MaxSize.
- Tente plusieurs ratios avant d’abandonner (feuille).
- Salle finale: taille réajustée aléatoirement dans les limites, puis marquage des cellules.

Logique de split:
```csharp
for (int i = 0; i < MaxSplitAttempt; i++) {
    bool horizontal = _randomService.Chance(HorizontalSplitChance);
    float ratio = _randomService.Range(SplitRatio.x, SplitRatio.y);
    if (horizontal ? CanSplitHorizontally(ratio, out a, out b)
                   : CanSplitVertically(ratio, out a, out b)) { splitFound = true; break; }
}
if (!splitFound) { 
    PlaceRoom(_room); return; }
_child1 = new Node(... a); _child2 = new Node(... b);
```
Connexion des feuilles (centres des derniers descendants):
```csharp
var c1 = node1.GetLastChild()._room.GetCenter();
var c2 = node2.GetLastChild()._room.GetCenter();
CreateDogLegCorridor(c1, c2);
```

## Tiles / Objets
Nom des templates utilisés (DB ScriptableObject):
- Room
- Corridor
- Grass / Water (cellular)

Ajouter un objet: `AddTileToCell(cell, ROOM_TILE_NAME, true);`

## Utilisation rapide
1. Ajouter le composant `ProceduralGridGenerator` sur un GameObject.
2. Assigner une méthode (un ScriptableObject créé via CreateAssetMenu).
3. Régler Seed, StepDelay, Dimensions de la grille .
4. Lancer la génération .

## Annulation / Pas à pas
- Chaque méthode peut être annulée (nouvelle génération relance un token). 
- `StepDelay` ralentit visuellement les étapes (utile pour debug ou vidéo).

## Idées d’extension
- Post-process pour lisser les couloirs.
- Placement d’items aléatoires sur cases "Room" uniquement.
- Génération de portes entre couloir et salle.

Fin. README volontairement court et direct.

