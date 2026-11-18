# Migration 

## Le problème : la dépendance qui "fuit" dans le domaine

```mermaid
flowchart TB
    subgraph domain["Domain"]
        RS["ReportService"]
    end

    subgraph Infra["Infrastructure"]
        Repository
    end

    Repository["ReportRepository"]

    SDK["SDK Azure Storage<br/>RequestFailedException"]

    domain --- Infra
    Infra ---|"référence directe<br/>types + exceptions du SDK"| SDK
    domain ---|"RequestFailedException"| SDK
```
## La solution : la couche anti corruption
### Etape 1
```mermaid
flowchart BT  
    subgraph acl["ACL / Infra "]
        ADP["AzureBlobStorageAdapterLegacy"]
    end

    subgraph ext["Externe"]
        SDK["SDK Azure Storage<br/>BlobContainerClient"]
    end    
    subgraph domain["Domain"]
        RS["ReportService"]
        IRS["IReportStorage<br/>aucune référence au SDK"]
        DSE["DomainStorageException"]
    end   
    
    RS --- IRS
    ADP -.-> IRS
    acl ---|"UploadAsync"| ext
    ext ---|"RequestFailedException"| acl
    ADP ---|"RequestFailedException traduite en"| DSE


```

### Etape 2
```mermaid
    flowchart TB
    subgraph domain["Domain"]
        RS["ReportService"]
        IRS["IReportStorage<br/>aucune référence au SDK"]
    end

    subgraph acl["ACL / Infra"]
        ADP["AzureBlobStorageAdapterLegacy"]
        SNI["AmazonStorageAdapter"]
    end


    subgraph Test["Test unitaire, test de caractérisation,<br>contrat de test"]
        IStorageContractTest
    end

    subgraph ext["Externe"]
        SDK["SDK Azure Storage<br/>BlobContainerClient"]
        SDKA["SDK Amazon Storage<br/>AmazonBlobContainerClient"]
    end

    RS ---IRS
    IRS -.-ADP
    IRS -.-SNI
    ADP ---|"UploadAsync"| SDK
    SNI ---|"UploadAsync"| SDKA

    IStorageContractTest ---|"Run test"| ADP
    IStorageContractTest ---|"Run test"| SNI

```

### autre vue, diagramme de séquence
```mermaid
sequenceDiagram
    participant RS as ReportService (Domain)
    participant I as IReportStorage (seam)
    participant A as AzureBlobStorageAdapter (ACL)
    participant S as SDK Azure Storage

    RS->>I: UploadAsync(path, content)
    I->>A: (implémentation injectée via DI)
    A->>S: GetBlobClient(path).UploadAsync(...)
    alt succès
        S-->>A: OK
        A-->>RS: Task terminé
    else RequestFailedException
        S-->>A: Exception SDK
        A->>A: Traduction
        A-->>RS: DomainStorageException
    end
```