# PRD – Soft Delete / Versioned CRUD API

## 1. Overview
Nom du produit : Soft Delete / Versioned CRUD API  
Type : Librairie .NET (NuGet)  
Cible : ASP.NET Core / EF Core / SQL Server  

Objectif : Fournir un mécanisme centralisé pour gérer des entités avec suppression douce (Soft Delete) et versioning complet, permettant audit, restauration et gestion d’historique des données.

---

## 2. Problème
- Suppression définitive accidentelle ou mal contrôlée
- Perte d’historique et difficultés de restauration
- Multiplication de code pour gérer versions et historique
- Difficulté à suivre les changements de données critiques

---

## 3. Objectifs
- Implémenter Soft Delete sur toutes les entités concernées
- Historiser chaque modification avec versioning complet (Versioned CRUD)
- Permettre restauration et rollback des entités
- Compatible EF Core, transactions et multi-tenant
- Extensible sans modifier le code métier existant

---

## 4. Concepts clés
### Entité versionnée
```csharp
public class VersionedEntity
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? DeletedBy { get; set; }
}
```

### Actions supportées
- Create (Version 1)
- Update (nouvelle version)
- Delete (Soft Delete, version conservée)
- Restore (revenir à version précédente)

### Optionnel
- Multi-tenant : `TenantId`
- Historisation des relations / collections
- Champs sensibles chiffrés

---

## 5. Stockage EF Core
- Table `Entities` versionnée avec colonne `Version`
- Champ `IsDeleted` pour Soft Delete
- Index sur `Id`, `Version`, `DeletedAt` pour requêtes rapides
- Optimisé pour inserts et mises à jour massives
- Option purge automatique ou archivage

---

## 6. API publique
### Service principal
```csharp
Task<T> CreateAsync<T>(T entity);
Task<T> UpdateAsync<T>(T entity);
Task SoftDeleteAsync<T>(Guid id, string userId);
Task RestoreAsync<T>(Guid id, int version);
Task<T?> GetAsync<T>(Guid id, bool includeDeleted = false);
Task<IEnumerable<T>> GetHistoryAsync<T>(Guid id);
```

### Middleware / DbContext Interceptor
- Intercepte `SaveChangesAsync` pour versioning et Soft Delete
- Compatible transactions EF Core
- Applique automatiquement `IsDeleted = true` sans supprimer la ligne

---

## 7. Sécurité
- Contrôle accès lecture/restauration/suppression par rôle
- Protection contre suppression définitive accidentelle
- Historique immuable
- Chiffrement optionnel des données sensibles

---

## 8. Performance
- Interception légère et asynchrone
- Inserts batch et mise à jour optimisée pour versioning
- Indexation pour récupération rapide de versions
- Option de désactivation du versioning pour certaines entités

---

## 9. Livrables
- Package NuGet `SoftTrack`
- README avec configuration et exemples d’intégration
- Tests unitaires et d’intégration
- Exemple ASP.NET Core complet avec DbContext interceptor

---

## 10. Évolutions futures
- Historisation relationnelle et collections
- Audit complet combiné avec Audit / Change Tracking API
- Dashboard de suivi des versions et suppression douce
- Support pour versioning automatique en cascade (entités liées)
- Export CSV / JSON pour archivage et compliance
