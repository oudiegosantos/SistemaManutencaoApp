using Npgsql;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors();
var app = builder.Build();

//Aceita qualquer tipo de requisição
app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());


string bancoDados = "Host=roundhouse.proxy.rlwy.net;Port=25886;Database=railway;Username=postgres;Password=YWRTDWmEsnFFBwKzSMYDIQYDOiLzAKLV;SSL Mode=Require;Trust Server Certificate=true";

using (var conn = new NpgsqlConnection(bancoDados))
{

conn.Open();
var tabClientes = conn.CreateCommand();
tabClientes.CommandText = @"
    CREATE TABLE IF NOT EXISTS Clientes (
        id SERIAL PRIMARY KEY,
        nome TEXT NOT NULL,
        telefone TEXT,
        endereco TEXT
)";
tabClientes.ExecuteNonQuery();
}

// LISTAR CLIENTES
app.MapGet("/clientes", () =>
{
    using var connection = new NpgsqlConnection(bancoDados);
    connection.Open();
    var lista = new List<object>();
    var cmd = connection.CreateCommand();
    cmd.CommandText = "SELECT * FROM Clientes";
    var cliente = cmd.ExecuteReader();
    while (cliente.Read())
    {
        lista.Add(new
        {
            id = cliente.GetInt32(0),
            nome = cliente.GetString(1),
            telefone = cliente.GetString(2),
            endereco = cliente.GetString(3)
        });
    }
    return lista;
});


//CADASTRAR CLIENTES
app.MapPost("/clientes", (Cliente cliente) =>
{
    using var connection = new NpgsqlConnection(bancoDados);
    connection.Open();
    var cmd = connection.CreateCommand();
    cmd.CommandText = "INSERT INTO Clientes(nome, telefone, endereco) VALUES(@nome, @telefone, @endereco)";
    cmd.Parameters.AddWithValue("@nome", cliente.nome);
    cmd.Parameters.AddWithValue("@telefone", cliente.telefone);
    cmd.Parameters.AddWithValue("@endereco", cliente.endereco);
    cmd.ExecuteNonQuery();
    return Results.Ok("Cliente cadastrado com sucesso!");
    
});

//DELETER CLIENTES 

app.MapDelete("/clientes/{id}", (int id) =>
{
    using var connection = new NpgsqlConnection(bancoDados);
    connection.Open();
    var cmd = connection.CreateCommand();
    cmd.CommandText = "DELETE FROM Clientes WHERE id=@id";
    cmd.Parameters.AddWithValue("@id", id);
    int clienteEncontrado = cmd.ExecuteNonQuery();
    return clienteEncontrado > 0 ? Results.Ok("Cliente deletado com sucesso!") : 
    Results.NotFound("Nenhum cliente encontrado");
});

// EDITAR CLIENTES 

app.MapPut("/clientes/{id}", (int id, Cliente cliente) =>
{
    using var connection = new NpgsqlConnection(bancoDados);
    connection.Open();
    var cmd = connection.CreateCommand();
    cmd.CommandText = "UPDATE Clientes SET nome=$nome, telefone=$telefone, endereco=$endereco WHERE id=@id";
    cmd.Parameters.AddWithValue("@nome", cliente.nome);
    cmd.Parameters.AddWithValue("@telefone", cliente.telefone);
    cmd.Parameters.AddWithValue("@endereco", cliente.endereco);
    cmd.Parameters.AddWithValue("@id", id);
    int clienteEncontrado = cmd.ExecuteNonQuery();
    return clienteEncontrado > 0 ? Results.Ok("Cliente atualizado com sucesso!"): Results.NotFound("Cliente não encontrado");
    
});

var port = Environment.GetEnvironmentVariable("PORT") ?? "5174";
app.Run($"http://0.0.0.0:{port}");
record Cliente(string nome, string telefone, string endereco);