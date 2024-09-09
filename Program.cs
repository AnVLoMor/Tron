using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;

public enum Direction
{
    Up,
    Down,
    Left,
    Right
}

public class Motorcycle
{
    public LinkedList<Cell> Trail { get; private set; }
    public int Speed { get; set; }
    public int Fuel { get; set; }
    public Queue<Item> Items { get; private set; }
    public Stack<Power> Powers { get; private set; }
    public bool IsDestroyed { get; set; }
    public Direction CurrentDirection { get; set; }

    public Motorcycle(int startX, int startY)
    {
        Speed = new Random().Next(1, 11);
        Fuel = 100;
        Trail = new LinkedList<Cell>();
        Items = new Queue<Item>();
        Powers = new Stack<Power>();
        IsDestroyed = false;
        CurrentDirection = Direction.Right;

        // Inicializa la moto y la estela
        Trail.AddLast(new Cell { X = startX, Y = startY });
        Trail.AddLast(new Cell { X = startX, Y = startY + 1 });
        Trail.AddLast(new Cell { X = startX, Y = startY + 2 });
        Trail.AddLast(new Cell { X = startX, Y = startY + 3 });
    }

    public void Move()
    {
        if (Fuel <= 0)
        {
            IsDestroyed = true;
            return;
        }

        Cell nextPosition = GetNextPosition(CurrentDirection);

        Trail.AddFirst(nextPosition);
        if (Trail.Count > 4) // Limita la estela a 3 posiciones más la cabeza
        {
            Trail.RemoveLast();
        }

        Fuel -= Speed / 5;
    }

    private Cell GetNextPosition(Direction direction)
    {
        Cell currentPosition = Trail.First.Value;

        switch (direction)
        {
            case Direction.Up:
                return new Cell { X = currentPosition.X, Y = currentPosition.Y - 1 };
            case Direction.Down:
                return new Cell { X = currentPosition.X, Y = currentPosition.Y + 1 };
            case Direction.Left:
                return new Cell { X = currentPosition.X - 1, Y = currentPosition.Y };
            case Direction.Right:
                return new Cell { X = currentPosition.X + 1, Y = currentPosition.Y };
            default:
                return currentPosition;
        }
    }

    public void UsePower()
    {
        if (Powers.Count > 0)
        {
            Powers.Pop().Activate(this);
        }
    }
}

public class Map
{
    public LinkedList<Cell> Grid { get; private set; }
    public List<Power> PowersOnMap { get; private set; }

    public Map(int width, int height)
    {
        Grid = new LinkedList<Cell>();
        PowersOnMap = new List<Power>();

        for (int i = 0; i < width * height; i++)
        {
            Grid.AddLast(new Cell());
        }
    }

    public void SpawnItemsAndPowers()
    {
        Random rnd = new Random();

        // Generar poderes aleatorios en posiciones aleatorias
        for (int i = 0; i < 10; i++) // Por ejemplo, 10 poderes
        {
            int x = rnd.Next(0, 20);
            int y = rnd.Next(0, 20);
            PowersOnMap.Add(new Power(Power.PowerType.Shield) { Location = new Cell { X = x, Y = y } });
        }
    }
}

public class Cell
{
    public int X { get; set; }
    public int Y { get; set; }
    public bool IsOccupied { get; set; }
}

public class Item
{
    public enum ItemType { Fuel, TrailGrowth, Bomb }
    public ItemType Type { get; private set; }

    public Item(ItemType type)
    {
        Type = type;
    }

    public void Apply(Motorcycle motorcycle)
    {
        switch (Type)
        {
            case ItemType.Fuel:
                motorcycle.Fuel = Math.Min(100, motorcycle.Fuel + new Random().Next(1, 21));
                break;
            case ItemType.TrailGrowth:
                // Logic to increase trail size
                break;
            case ItemType.Bomb:
                motorcycle.IsDestroyed = true;
                break;
        }
    }
}

public class Power
{
    public enum PowerType { Shield, HyperSpeed }
    public PowerType Type { get; private set; }
    public Cell Location { get; set; }

    public Power(PowerType type)
    {
        Type = type;
    }

    public void Activate(Motorcycle motorcycle)
    {
        switch (Type)
        {
            case PowerType.Shield:
                // Logic to make the motorcycle invincible
                break;
            case PowerType.HyperSpeed:
                motorcycle.Speed += new Random().Next(1, 6);
                break;
        }
    }
}

public class Game
{
    private Map map;
    private List<Motorcycle> motorcycles;
    private Motorcycle playerMotorcycle;

    public Game(int width, int height, int numPlayers)
    {
        map = new Map(width, height);
        motorcycles = new List<Motorcycle>();

        // Inicializa la moto del jugador en una posición fija
        playerMotorcycle = new Motorcycle(5, 5);
        motorcycles.Add(playerMotorcycle);

        // Inicializa los enemigos (bots)
        for (int i = 1; i < numPlayers; i++)
        {
            motorcycles.Add(new Motorcycle(5 + i * 3, 5)); // Ejemplo: motos en filas distintas
        }

        map.SpawnItemsAndPowers();
    }

    public List<Motorcycle> GetMotorcycles()
    {
        return motorcycles;
    }

    public Motorcycle GetPlayerMotorcycle()
    {
        return playerMotorcycle;
    }

    // Nuevo método GetMap para acceder al mapa
    public Map GetMap()
    {
        return map;
    }

    public void Update()
    {
        foreach (var motorcycle in motorcycles)
        {
            if (!motorcycle.IsDestroyed)
            {
                motorcycle.Move();
            }
        }
    }
}

public class GameForm : Form
{
    private Game game;
    private Timer gameTimer;

    public GameForm(Game gameInstance)
    {
        this.game = gameInstance;
        this.DoubleBuffered = true;
        this.KeyDown += new KeyEventHandler(OnKeyDown);
        this.Width = 800;
        this.Height = 600;

        gameTimer = new Timer();
        gameTimer.Interval = 100; // 10 cuadros por segundo
        gameTimer.Tick += new EventHandler(UpdateGame);
        gameTimer.Start();
    }

    private void UpdateGame(object sender, EventArgs e)
    {
        game.Update();
        this.Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        Graphics g = e.Graphics;

        foreach (var motorcycle in game.GetMotorcycles())
        {
            foreach (var cell in motorcycle.Trail)
            {
                g.FillRectangle(Brushes.Blue, new Rectangle(cell.X * 20, cell.Y * 20, 20, 20));
            }
        }

        // Dibujar poderes en el mapa
        foreach (var power in game.GetMap().PowersOnMap)
        {
            g.FillRectangle(Brushes.Red, new Rectangle(power.Location.X * 20, power.Location.Y * 20, 20, 20));
        }
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        Motorcycle playerMotorcycle = game.GetPlayerMotorcycle();

        switch (e.KeyCode)
        {
            case Keys.Up:
                playerMotorcycle.CurrentDirection = Direction.Up;
                break;
            case Keys.Down:
                playerMotorcycle.CurrentDirection = Direction.Down;
                break;
            case Keys.Left:
                playerMotorcycle.CurrentDirection = Direction.Left;
                break;
            case Keys.Right:
                playerMotorcycle.CurrentDirection = Direction.Right;
                break;
        }
    }
}

public static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Game game = new Game(20, 20, 5); // 1 jugador + 4 enemigos
        Application.Run(new GameForm(game)); // Pasa la instancia de game a GameForm
    }
}
