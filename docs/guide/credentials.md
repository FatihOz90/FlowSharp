# Credentials

Credentials hold the secrets your nodes need to reach external services — API keys, database passwords, SMTP logins, OAuth tokens. FlowSharp stores them **encrypted at rest**, scopes them to their owner, and resolves them only at execution time.

## How credentials are stored

Each credential is a row in the `credentials` table with a `Name`, a `Type` (the credential type key, e.g. `openAiApi`, `postgres`, `smtp`), an `OwnerId`, and an `EncryptedData` blob. Sensitive fields are serialized to JSON and encrypted with **AES-GCM** using `Security:CredentialEncryptionKey`; plaintext is never written to the database.

```text
Credential
├─ Name           "My OpenAI key"
├─ Type           "openAiApi"
├─ OwnerId        <Identity user id>
└─ EncryptedData  AES-GCM(nonce + ciphertext + tag), Base64
```

::: danger The encryption key is critical
`Security:CredentialEncryptionKey` (Base64, 32 bytes) must be **identical across the Web and Worker processes** and **stable over time**. If it changes, existing credentials can no longer be decrypted. Supply it through a secret/environment variable in production — never rely on the sample key. See [Configuration](configuration.md#security).
:::

## Credential types and schemas

A credential **type** describes its fields with a [`CredentialSchema`](https://github.com/FlowSharp/FlowSharp/blob/main/src/FlowSharp.Domain/Credentials/CredentialSchema.cs). The UI renders inputs from the schema — there is no guessing from field names. Each field has a type that controls rendering:

| `CredentialFieldType` | Renders as |
|---|---|
| `String` | Text input |
| `Secret` | Password input (write-only) |
| `Boolean` | Checkbox |
| `Number` | Numeric input |

Nodes declare the credential types they use in two ways:

- **`IProvidesCredentials`** — a node declares its credential schema(s) **next to itself**. This is the common path; the schema lives with the node that uses it (fully dynamic, no central registry). Example: the database connection nodes expose a `Database`/`SqlServer` schema.
- **`ICredentialType`** — a standalone credential type not tied to a single node (useful for pure plugins).

All schemas are aggregated into the `ICredentialCatalog`, which the Credentials UI and validation read from.

### Reusable field patterns

[`CredentialFields`](https://github.com/FlowSharp/FlowSharp/blob/main/src/FlowSharp.Nodes/Credentials/CredentialFields.cs) provides common field sets so node authors don't repeat themselves (it is a helper, **not** a central registry — a node picks and customizes what it needs):

```csharp
CredentialFields.ApiKey()     // apiKey (Secret, required)
CredentialFields.Database()   // host, port, user, password, ssl
CredentialFields.SqlServer()  // Database() + integratedSecurity
CredentialFields.Mail()       // host, port, user, password, secure
```

## Using a credential in a node

In the node definition, a `Credential` parameter (or the `Credentials` list referencing a type key) tells the designer to show a credential picker. At runtime the node reads a field with:

```csharp
// type = credential type key, name = selected credential, field = field key
var apiKey = await context.GetCredentialAsync("openAiApi", credentialName, "apiKey");
```

The connection-style database nodes go further: a **Connection** node (`db.postgres.connection`, etc.) selects a credential once and passes a connection context to downstream `db.*` operation nodes. See [Built-in Nodes](built-in-nodes.md#database).

## Ownership and isolation

Credentials carry an `OwnerId`:

- Non-administrators (`Editor`, `Member`) see and manage **only the credentials they created**.
- `Admin` can manage all credentials.
- At execution time a credential is resolved **only when its owner matches the workflow owner**, preventing cross-tenant secret access.

This makes self-registration safe in multi-user deployments — each user works within an isolated credential workspace. See [Roles & Permissions](roles-and-permissions.md#data-isolation-ownership).

The `credentials.manage` permission is required to create, edit, delete, or view stored credentials.

## Operational guidance

- **Back up the encryption key** together with the database. A database restore is useless without the matching key.
- **Rotating provider secrets** (e.g. a leaked API key) is just editing the credential — the encryption key stays the same.
- **Rotating the encryption key** is a migration exercise: decrypt with the old key and re-encrypt with the new one before switching. There is no automatic re-keying.
