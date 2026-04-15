const API = "http://localhost:5174";

// CADASTRAR CLIENTE
async function cadastrarCliente() {
    const nome = document.getElementById("nome").value;
   // document.getElementById("nome").style.backgroundColor = "Red"
    const telefone = document.getElementById("telefone").value;
    const endereco = document.getElementById("endereco").value;

    if(!nome) {alert("Nome é origatório!"); return};

    await fetch(`${API}/clientes`, {
        method: "POST",
        headers: {"content-Type": "application/json"},
        body: JSON.stringify({nome, telefone, endereco})

    });

    alert("Cliente cadastrado!");
    listarClientes();
    
};

// LISTAR CLIENTES

async function listarClientes() {
    const res = await fetch(`${API}/clientes`);
    const clientes = await res.json();
    const tbody = document.getElementById("tabelaClientes");
    tbody.innerHTML = "";
    clientes.forEach(c => {
        tbody.innerHTML += `
            <tr>
                <td>${c.id}</td>
                <td>${c.nome}</td>
                <td>${c.telefone}</td>
                <td>${c.endereco}</td>
                <td>
                   <button onclick="deletarCliente(${c.id})">Deletar</button>
                   <button onclick="editarCliente(${c.id}, '${c.nome}', '${c.telefone}', '${c.endereco}')">Editar</button>
                </td>
            </tr>
        `;
    });
}

//DELETAR CLIENTES 

async function deletarCliente(id) {
    if (!confirm("Deseja deletar o cliente?")) 
        return;
    await fetch(`${API}/clientes/${id}`, {
        method: "DELETE"
    });
    listarClientes();
}

// EDITAR CLIENTES

async function editarCliente(id, nome, telefone, endereco) {
    const novoNome = prompt("Digite o novo nome", nome);
    const novoTelefone = prompt("Digite o novo telefone", telefone);
    const novoEndereco = prompt("Digite o novo Endereço", endereco);

    await fetch(`${API}/clientes/${id}`, {
        method: "PUT",
        headers: {"Content-Type": "application/json"},
        body: JSON.stringify({nome: novoNome, telefone: novoTelefone, endereco: novoEndereco})
    });

    listarClientes();
}
listarClientes();