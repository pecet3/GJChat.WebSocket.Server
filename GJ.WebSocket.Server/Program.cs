using System.Net;
using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseWebSockets();

Console.WriteLine("Start...");

var connections = new List<WebSocket>();

app.Map("/", () => "Hello");

app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var connName = context.Request.Query["name"];
        using var conn = await context.WebSockets.AcceptWebSocketAsync();
        connections.Add(conn);

        await Broadcast($"{connName}  do³¹czy³ na serwer!");
        await Broadcast($"{connections.Count} u¿ytkowników na serwerze.");

        await ReceiveMessage(conn, async (result, buffer) =>
        {
            if (conn.State == WebSocketState.Open)
            {
                string message = Encoding.UTF8.GetString(buffer);

                await Broadcast($"{connName}: {message}");
            }

            if (conn.State == WebSocketState.Aborted || conn.State == WebSocketState.Closed || result.MessageType == WebSocketMessageType.Close)
            {
                connections.Remove(conn);
                await Broadcast($"{connName} opuœci³ serwer.");
                await Broadcast($"{connections.Count} u¿ytkowników na serwerze.");
                await conn.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            }
        });

    }
    else
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
    }
});

async Task ReceiveMessage(WebSocket conn, Action<WebSocketReceiveResult, byte[]> handleMessage)
{
    var buffer = new byte[1024 * 4];

    try
    {
        while (conn.State == WebSocketState.Open)
        {
            var result = await conn.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            handleMessage(result, buffer);
        }
    }
    catch (WebSocketException)
    {
        connections.Remove(conn);
    }
}
{

}

async Task Broadcast(string message)
{
    var bytes = Encoding.UTF8.GetBytes(message);
    var arraySegment = new ArraySegment<byte>(bytes, 0, bytes.Length);

    foreach (var conn in connections)
    {
        if (conn.State == WebSocketState.Open)
        {
            await conn.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}

await app.RunAsync();
