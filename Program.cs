using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors();
var app = builder.Build();

//Aceita qualquer tipo de requisição
app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());


string bancoDados = "Host=aws-1-us-west-2.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.xgrvegqbctqqpnsebgha;Password=JCSempre1919;SSL Mode=Require;Trust Server Certificate=true";

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

// TABELA DE USUARIOS

using (var conn = new NpgsqlConnection(bancoDados))
{
    conn.Open();
    var tabUsuarios = conn.CreateCommand();
    tabUsuarios.CommandText = @"CREATE TABLE IF NOT EXISTS Usuarios (
        id SERIAL PRIMARY KEY,
        nome TEXT NOT NULL,
        email TEXT NOT NULL UNIQUE,
        senha TEXT NOT NULL,
        perfil TEXT NOT NULL DEFAULT 'usuario'
    )";
    tabUsuarios.ExecuteNonQuery();
}


// REGISTAR USUÁRIO 
app.MapPost("auth/registrar", (Usuario usuario) =>
{
    using var conn = new NpgsqlConnection(bancoDados);
    conn.Open();

    var vExistUser = conn.CreateCommand();
    vExistUser.CommandText = "SELECT COUNT(*) FROM Usuarios WHERE email=@email";
    vExistUser.Parameters.AddWithValue("@email", usuario.email);
    long vExist = (long)(vExistUser.ExecuteScalar() ?? 0L);
    if (vExist > 0) return Results.BadRequest("Usuário já existente!");

    string passowrdCripto = BCrypt.Net.BCrypt.HashPassword(usuario.senha);

    var cmd = conn.CreateCommand();
    cmd.CommandText = "INSERT INTO Usuarios(nome, email, senha, perfil) VALUES(@nome, @email, @senha, @perfil)";
    cmd.Parameters.AddWithValue("@nome", usuario.nome);
    cmd.Parameters.AddWithValue("@email", usuario.email);
    cmd.Parameters.AddWithValue("@senha", passowrdCripto);
    cmd.Parameters.AddWithValue("@perfil", usuario.perfil);

    cmd.ExecuteNonQuery();
    return Results.Ok("Usuário cadastrado!");

});

// LOGIN USUARIO

app.MapPost("auth/login", (Login login) =>
{
    using var conn = new NpgsqlConnection(bancoDados);
    conn.Open();

    var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT id, nome, email, senha, perfil FROM Usuarios WHERE email = @email";
    cmd.Parameters.AddWithValue("@email", login.email);
    var len = cmd.ExecuteReader();

    if(!len.Read())
    return Results.Unauthorized();

    string passData = len.GetString(3);
    if(!BCrypt.Net.BCrypt.Verify(login.senha, passData))
    return Results.Unauthorized();

    int id = len.GetInt32(0);
    string nome = len.GetString(1);
    string perfil = len.GetString(4);

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("metalurgica-montenegro-chave-secreta-2026"));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var claims = new[]
    {
        new Claim("id", id.ToString()),
        new Claim("nome", nome),
        new Claim("perfil", perfil)

    };

    var token = new JwtSecurityToken(

        expires: DateTime.UtcNow.AddHours(8),
        signingCredentials: creds,
        claims: claims
    );

    string tokenString = new JwtSecurityTokenHandler().WriteToken(token);

    return Results.Ok(new {token = tokenString, nome, perfil});

});



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
    cmd.CommandText = "UPDATE Clientes SET nome=@nome, telefone=@telefone, endereco=@endereco WHERE id=@id";
    cmd.Parameters.AddWithValue("@nome", cliente.nome);
    cmd.Parameters.AddWithValue("@telefone", cliente.telefone);
    cmd.Parameters.AddWithValue("@endereco", cliente.endereco);
    cmd.Parameters.AddWithValue("@id", id);
    int clienteEncontrado = cmd.ExecuteNonQuery();
    return clienteEncontrado > 0 ? Results.Ok("Cliente atualizado com sucesso!"): Results.NotFound("Cliente não encontrado");
    
});

var port = Environment.GetEnvironmentVariable("PORT") ?? "5174";
app.Run($"http://0.0.0.0:{port}");

record Usuario(string nome, string email, string senha, string perfil);
record Login(string email, string senha);
record Cliente(string nome, string telefone, string endereco);
