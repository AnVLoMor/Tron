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

    private int startX;
    private int startY;

    public Motorcycle(int startX, int startY)
    {
        this.startX = startX;
        this.startY = startY;

        Speed = new Random().Next(1, 11);
        Fuel = 100;
        Trail = new LinkedList<Cell>();
        Items = new Queue<Item>();
        Powers = new Stack<Power>();
        IsDestroyed = false;

        Trail.AddLast(new Cell { X = startX, Y = startY });
        Trail.AddLast(new Cell { X = startX, Y = startY + 1 });
        Trail.AddLast(new Cell { X = startX, Y = startY + 2 });
    }

    public void Move(Direction direction)
    {
        if (Fuel <= 0)
        {
            IsDestroyed = true;
            return;
        }

        Cell nextPosition = GetNextPosition(direction);

        Trail.AddFirst(nextPosition);
        if (Trail.Count > 3)
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

    public Map(int width, int height)
    {
        Grid = new LinkedList<Cell>();
        for (int i = 0; i < width * height; i++)
        {
            Grid.AddLast(new Cell());
        }
    }

    public void SpawnItemsAndPowers()
    {
        // Logic to spawn items and powers randomly on the map
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

    public Game(int width, int height, int numPlayers)
    {
        map = new Map(width, height);
        motorcycles = new List<Motorcycle>();

        for (int i = 0; i < numPlayers; i++)
        {
            motorcycles.Add(new Motorcycle(5 + i * 3, 5)); // Ejemplo: motos en filas distintas
        }

        map.SpawnItemsAndPowers();
    }

    public List<Motorcycle> GetMotorcycles()
    {
        return motorcycles;
    }

    public void Update()
    {
        foreach (var motorcycle in motorcycles)
        {
            if (!motorcycle.IsDestroyed)
            {
                motorcycle.Move(Direction.Right); // Mueve las motos hacia la derecha por ahora
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
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        // Manejar la entrada del teclado para mover las motos
    }
}

public static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Game game = new Game(20, 20, 4); // Crear instancia de Game
        Application.Run(new GameForm(game)); // Pasa la instancia de game a GameForm
    }
}
