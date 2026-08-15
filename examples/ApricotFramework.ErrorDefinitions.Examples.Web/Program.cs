using ApricotFramework.ErrorDefinitions;
using ApricotFramework.ErrorDefinitions.AspNetCore.Extensions;
using ApricotFramework.ErrorDefinitions.Examples.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Everything error-related in one call. Nothing to configure: what a service answers when it fails is
// part of its contract, so it is not something an environment gets to change.
builder.Services.AddErrorDefinitions();

// A failure that knows more than its classification gets a mapper of its own, so the reason reaches the
// response instead of being flattened into a message.
builder.Services.AddExceptionErrorMapper<CaptchaRejectedMapper>();

// A failure that is only ever one kind needs no class at all.
builder.Services.MapExceptionToError<InvalidTimeZoneException>(
    ErrorKinds.Unavailable,
    DemoErrors.LegacySubsystemFailed);

// Used by the gateway action to call this same service, standing in for one service calling another.
builder.Services.AddHttpClient("self", client => client.BaseAddress = new Uri("http://localhost:5241"));

var app = builder.Build();

// Where this sits is the host's decision, which is why the library does not add it — and it is the one
// piece of ordering that still matters: anything throwing before this line is not standardised.
app.UseExceptionHandler();

app.MapControllers();

app.Run();
