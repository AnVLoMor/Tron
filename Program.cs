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
    public bool IsPlayer { get; set; }

    public Motorcycle(int startX, int startY, bool isPlayer)
    {
        Speed = new Random().Next(1, 11);
        Fuel = 100;
        Trail = new LinkedList<Cell>();
        Items = new Queue<Item>();
        Powers = new Stack<Power>();
        IsDestroyed = false;
        CurrentDirection = Direction.Right;
        IsPlayer = isPlayer;

        // Initialize the motorcycle and its trail
        Trail.AddLast(new Cell { X = startX, Y = startY });
        Trail.AddLast(new Cell { X = startX, Y = startY + 1 });
        Trail.AddLast(new Cell { X = startX, Y = startY + 2 });
        Trail.AddLast(new Cell { X = startX, Y = startY + 3 });
    }

    public void Move(Map map)
    {
        if (Fuel <= 0)
        {
            IsDestroyed = true;
            return;
        }

        Cell nextPosition = GetNextPosition(CurrentDirection);

        // Check if the next position is out of bounds
        if (nextPosition.X < 0 || nextPosition.X >= map.Width || nextPosition.Y < 0 || nextPosition.Y >= map.Height)
        {
            IsDestroyed = true;
            return;
        }

        // Check if colliding with own trail
        foreach (var cell in Trail)
        {
            if (nextPosition.X == cell.X && nextPosition.Y == cell.Y)
            {
                IsDestroyed = true;
                return;
            }
        }

        Trail.AddFirst(nextPosition);
        if (Trail.Count > 4) // Limit the trail to 3 positions plus the head
        {
            Trail.RemoveLast();
        }

        Fuel -= Speed / 20; // Reduced fuel consumption
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
    public int Width { get; private set; }
    public int Height { get; private set; }

    public Map(int width, int height)
    {
        this.Width = width;
        this.Height = height;
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

        // Generate random powers in random positions
        for (int i = 0; i < 10; i++)
        {
            int x = rnd.Next(0, Width);
            int y = rnd.Next(0, Height);
            PowersOnMap.Add(new Power(Power.PowerType.Shield) { Location = new Cell { X = x, Y = y } });
        }
    }

    public Power CheckPowerCollision(Cell position)
    {
        Power collidedPower = null;

        foreach (var power in PowersOnMap)
        {
            if (power.Location.X == position.X && power.Location.Y == position.Y)
            {
                collidedPower = power;
                break;
            }
        }

        if (collidedPower != null)
        {
            PowersOnMap.Remove(collidedPower); // Remove the power from the map
        }

        return collidedPower;
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

        // Initialize the player's motorcycle in a fixed position
        playerMotorcycle = new Motorcycle(5, 5, true);
        motorcycles.Add(playerMotorcycle);

        // Initialize enemy motorcycles in random positions
        Random rnd = new Random();
        for (int i = 1; i < numPlayers; i++)
        {
            int x = rnd.Next(0, width);
            int y = rnd.Next(0, height);
            motorcycles.Add(new Motorcycle(x, y, false));
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
                motorcycle.Move(map);

                // Check for collisions with powers
                var collidedPower = map.CheckPowerCollision(motorcycle.Trail.First.Value);
                if (collidedPower != null)
                {
                    collidedPower.Activate(motorcycle);
                }

                // Check for collisions with other motorcycles
                foreach (var otherMotorcycle in motorcycles)
                {
                    if (motorcycle != otherMotorcycle && !otherMotorcycle.IsDestroyed)
                    {
                        foreach (var cell in otherMotorcycle.Trail)
                        {
                            if (motorcycle.Trail.First.Value.X == cell.X && motorcycle.Trail.First.Value.Y == cell.Y)
                            {
                                motorcycle.IsDestroyed = true;
                                break;
                            }
                        }
                    }
                }
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

        // Set a fixed size for the window based on the map size
        this.Width = gameInstance.GetMap().Width * 20;
        this.Height = gameInstance.GetMap().Height * 20;

        gameTimer = new Timer();
        gameTimer.Interval = 100; // 10 frames per second
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
            Brush brush = motorcycle.IsPlayer ? Brushes.Blue : Brushes.Yellow;
            foreach (var cell in motorcycle.Trail)
            {
                g.FillRectangle(brush, new Rectangle(cell.X * 20, cell.Y * 20, 20, 20));
            }
        }

        // Draw powers on the map
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
        Game game = new Game(50, 40, 5); // 1 player + 4 enemies
        Application.Run(new GameForm(game)); // Pass the game instance to GameForm
    }
}
