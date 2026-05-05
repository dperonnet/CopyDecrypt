## Objectif
Maintenir CopyDecrypt **simple, robuste et facile à faire évoluer**. Les changements doivent rester reviewables et éviter les heuristiques fragiles.

## Principes de revue (toujours appliquer)
- **Lisibilité d’abord** : noms explicites, code linéaire, peu de magie.
- **Responsabilités claires** : UI (WinForms) séparée de la logique (OCR/QR/clipboard/settings).
- **Défensif** : gérer erreurs/edge cases sans popups intempestives (sauf UX intentionnelle).
- **Pas de “devinettes” destructrices** sur le texte OCR : préférer amélioration de l’image en amont ou heuristiques strictement opt‑in et limitées.
- **Compatibilité “clé en main”** : pas de dépendances lourdes sans option/justification claire.

## Conventions C# (style et structure)
- **Nullable** : ne jamais masquer un avertissement par `!` sans justification.
- **Exceptions** : attraper au plus près, message utilisateur clair, journalisation minimale si utile.
- **`internal` par défaut**, `public` seulement si nécessaire.
- **Petites fonctions** (idéalement < 40 lignes), éviter les classes “god”.

## Commentaires (exigence)
- Commenter uniquement le **pourquoi** (intentions, contraintes Win32/WinForms, raisons UX).
- Éviter les commentaires qui répètent le code (“on ajoute X”, “on retourne Y”).
- Ajouter un commentaire quand un comportement semble étrange (ex: `Thread.Sleep` pour capture écran).

## Checklist à chaque changement
- **Build** : `dotnet build -c Debug` passe.
- **Cohérence UX** : tray/menu/options/hotkey cohérents.
- **Docs** : si la structure/flux change, mettre à jour `docs/architecture.md`.

