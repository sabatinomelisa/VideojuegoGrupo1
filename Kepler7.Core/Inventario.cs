namespace Kepler7.Core;

//Patrón: visitor
// Cada ítem del inventario "acepta" a un visitante. Así se pueden agregar
// operaciones nuevas (precio, conteo, veneno) sin modificar las clases de los ítems.

// El visitante tiene un método Visitar para cada tipo de ítem.
public interface IVisitor
{
    void Visitar(Arma arma);
    void Visitar(Pocion pocion);
    void Visitar(Armadura armadura);
    string Resultado();
}


public interface IItem
{
    string Nombre { get; }
    int Precio { get; }
    string Descripcion();
    void Aceptar(IVisitor visitante);
}


public class Arma : IItem
{
    public string Nombre { get; }
    public int Precio { get; }
    public int Danio { get; }
    public bool Envenenada { get; set; }

    public Arma(string nombre, int precio, int danio)
    {
        Nombre = nombre;
        Precio = precio;
        Danio = danio;
    }

    public string Descripcion()
    {
        string texto = "Arma · daño " + Danio;
        if (Envenenada)
        {
            texto = texto + " · ENVENENADA";
        }
        return texto;
    }

    public void Aceptar(IVisitor visitante)
    {
        visitante.Visitar(this);
    }
}

public class Pocion : IItem
{
    public string Nombre { get; }
    public int Precio { get; }
    public int Curacion { get; }

    public Pocion(string nombre, int precio, int curacion)
    {
        Nombre = nombre;
        Precio = precio;
        Curacion = curacion;
    }

    public string Descripcion()
    {
        return "Poción · cura " + Curacion;
    }

    public void Aceptar(IVisitor visitante)
    {
        visitante.Visitar(this);
    }
}

public class Armadura : IItem
{
    public string Nombre { get; }
    public int Precio { get; }
    public int Defensa { get; }

    public Armadura(string nombre, int precio, int defensa)
    {
        Nombre = nombre;
        Precio = precio;
        Defensa = defensa;
    }

    public string Descripcion()
    {
        return "Armadura · defensa " + Defensa;
    }

    public void Aceptar(IVisitor visitante)
    {
        visitante.Visitar(this);
    }
}


public class CalculadorPrecio : IVisitor
{
    private int total;

    public void Visitar(Arma arma)
    {
        total = total + arma.Precio;
    }

    public void Visitar(Pocion pocion)
    {
        total = total + pocion.Precio;
    }

    public void Visitar(Armadura armadura)
    {
        total = total + armadura.Precio;
    }

    public string Resultado()
    {
        return "Valor total del inventario: " + total + " créditos.";
    }
}


public class ContadorArmas : IVisitor
{
    private int armas;
    private int envenenadas;

    public void Visitar(Arma arma)
    {
        armas++;
        if (arma.Envenenada)
        {
            envenenadas++;
        }
    }

    public void Visitar(Pocion pocion)
    {
        // Las pociones no son armas: no se cuentan.
    }

    public void Visitar(Armadura armadura)
    {
        // Las armaduras no son armas: no se cuentan.
    }

    public string Resultado()
    {
        return "Hay " + armas + " armas en el inventario (" + envenenadas + " envenenadas).";
    }
}


public class AplicadorVeneno : IVisitor
{
    private int envenenadas;

    public void Visitar(Arma arma)
    {
        if (!arma.Envenenada)
        {
            arma.Envenenada = true;
            envenenadas++;
        }
    }

    public void Visitar(Pocion pocion)
    {
        // El veneno no se aplica a las pociones.
    }

    public void Visitar(Armadura armadura)
    {
        // El veneno no se aplica a las armaduras.
    }

    public string Resultado()
    {
        if (envenenadas == 0)
        {
            return "Todas las armas ya estaban envenenadas.";
        }
        return "Se envenenaron " + envenenadas + " armas. ¡Cuidado, mutantes!";
    }
}


public class Inventario
{
    public List<IItem> Items { get; } = new List<IItem>();

    public Inventario()
    {
        Items.Add(new Arma("Láser de bigotes", 120, 15));
        Items.Add(new Arma("Garra de plasma", 90, 22));
        Items.Add(new Arma("Ovillo explosivo", 60, 30));
        Items.Add(new Pocion("Leche curativa", 40, 30));
        Items.Add(new Pocion("Antídoto de hierba gatera", 55, 10));
        Items.Add(new Armadura("Traje espacial felino", 150, 10));
        Items.Add(new Armadura("Casco antimutantes", 80, 5));
    }

    public string Aplicar(IVisitor visitante)
    {
        foreach (IItem item in Items)
        {
            item.Aceptar(visitante);
        }
        return visitante.Resultado();
    }
}