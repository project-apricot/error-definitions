# ApricotFramework.ErrorDefinitions

[![NuGet](https://img.shields.io/nuget/v/ApricotFramework.ErrorDefinitions.svg?label=ApricotFramework.ErrorDefinitions)](https://www.nuget.org/packages/ApricotFramework.ErrorDefinitions/)
[![NuGet](https://img.shields.io/nuget/v/ApricotFramework.ErrorDefinitions.AspNetCore.svg?label=ApricotFramework.ErrorDefinitions.AspNetCore)](https://www.nuget.org/packages/ApricotFramework.ErrorDefinitions.AspNetCore/)
[![CI](https://github.com/project-apricot/error-definitions/actions/workflows/ci.yml/badge.svg)](https://github.com/project-apricot/error-definitions/actions/workflows/ci.yml)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](https://github.com/project-apricot/error-definitions/blob/main/LICENSE)

One error contract for a fleet of services: throw a classified error anywhere, and every service and
client reads the same RFC 9457 problem document back — including when the peer is not one of yours.

`ApricotFramework.ErrorDefinitions` is the **zero-dependency** core.

## Install

```bash
dotnet add package ApricotFramework.ErrorDefinitions
dotnet add package ApricotFramework.ErrorDefinitions.AspNetCore
```

## Usage

```csharp
using ApricotFramework.ErrorDefinitions.AspNetCore.Extensions;

builder.Services.AddErrorDefinitions();      // nothing to configure
app.UseExceptionHandler();
```

```csharp
// Throwing: the kind decides the status, the code decides the text a client shows.
var author = Ensure.Found(await repository.Get(id), ContentErrors.AuthorNotFound);

throw Err.Validation(ContentErrors.InvalidLocale, payload: new Dictionary<string, object?>
{
    ["locale"] = locale,          // the client renders its own message from the code and these
}).AsException();

// Reporting several at once, so a caller fixing a form submits it once.
new ErrorCollector()
    .AddIf(string.IsNullOrWhiteSpace(input.Name), Err.Validation(ContentErrors.InvalidName))
    .AddIf(!input.Email.Contains('@'), Err.Validation(ContentErrors.InvalidEmail))
    .ThrowIfAny();
```

```jsonc
// What the caller receives — application/problem+json
{
  "type": "about:blank",
  "title": "Validation failed",
  "status": 400,
  "detail": "That locale is not published.",
  "instance": "/api/content",
  "errors": [
    { "kind": "validation", "code": "CONTENT_INVALID_LOCALE",
      "message": "That locale is not published.", "payload": { "locale": "fr" } }
  ]
}
```

```csharp
// Reading another service's failure, from the zero-dependency core.
using var response = await client.GetAsync(uri, cancellationToken);

await response.EnsureNoErrorsAsync(cancellationToken);   // rethrows the peer's kind and code as your own
```

An **unmapped exception never reaches the caller**: it is reported as `internal` with no message, no
exception type and no stack trace, because that text routinely holds connection strings and SQL. The
exception goes to the log at error level instead, and that log is the only record of it — the
framework's own middleware does not log an exception once a handler has answered it.

The sixteen kinds are `google.rpc.Code` under names that read better over HTTP, so the same
classification maps to gRPC without a second table. `schemas/` publishes the whole contract as JSON
for clients that are not .NET.

Full documentation: <https://projectapricot.dev/docs/error-definitions>
