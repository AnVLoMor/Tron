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
    private int movesSinceLastFuelDecrease;

    public Motorcycle(int startX, int startY, bool isPlayer)
    {
        Speed = new Random().Next(1, 11);
        Fuel = 100; // Always start with 100 fuel
        Trail = new LinkedList<Cell>();
        Items = new Queue<Item>();
        Powers = new Stack<Power>();
        IsDestroyed = false;
        CurrentDirection = (Direction)new Random().Next(4); // Random initial direction
        IsPlayer = isPlayer;
        movesSinceLastFuelDecrease = 0;

        // Initialize the motorcycle and its trail
        Trail.AddLast(new Cell { X = startX, Y = startY });
        Trail.AddLast(new Cell { X = startX, Y = startY + 1 });
        Trail.AddLast(new Cell { X = startX, Y = startY + 2 });
        Trail.AddLast(new Cell { X = startX, Y = startY + 3 });
    }

    public void Move(Map map)
    {
        if (Fuel <= 0 || IsDestroyed)
        {
            IsDestroyed = true;
            return;
        }

        if (!IsPlayer)
        {
            // Intentar cambiar la dirección aleatoriamente hasta encontrar una que no lleve a un choque
            Direction originalDirection = CurrentDirection;
            for (int i = 0; i < 4; i++)
            {
                CurrentDirection = (Direction)new Random().Next(4);
                Cell nextPosition = GetNextPosition(CurrentDirection);

                // Comprobar si la siguiente posición está fuera de los límites o colisiona con alguna estela
                if (nextPosition.X >= 0 && nextPosition.X < map.Width && nextPosition.Y >= 0 && nextPosition.Y < map.Height)
                {
                    bool collision = false;

                    foreach (var motorcycle in map.Motorcycles)
                    {
                        foreach (var cell in motorcycle.Trail)
                        {
                            if (nextPosition.X == cell.X && nextPosition.Y == cell.Y)
                            {
                                collision = true;
                                break;
                            }
                        }
                        if (collision) break;
                    }

                    if (!collision)
                    {
                        break; // Encontró una dirección válida, salir del bucle
                    }
                }
            }

            // Si después de intentar cambiar dirección no encontró una válida, mantener la dirección original
            if (CurrentDirection == originalDirection)
            {
                Cell originalNextPosition = GetNextPosition(CurrentDirection);
                if (originalNextPosition.X < 0 || originalNextPosition.X >= map.Width || originalNextPosition.Y < 0 || originalNextPosition.Y >= map.Height)
                {
                    IsDestroyed = true;
                    return;
                }
            }
        }

        Cell finalPosition = GetNextPosition(CurrentDirection);

        // Check if the final position is out of bounds
        if (finalPosition.X < 0 || finalPosition.X >= map.Width || finalPosition.Y < 0 || finalPosition.Y >= map.Height)
        {
            IsDestroyed = true;
            return;
        }

        // Check if colliding with any trail (including own trail)
        foreach (var motorcycle in map.Motorcycles)
        {
            foreach (var cell in motorcycle.Trail)
            {
                if (motorcycle != this || !cell.Equals(Trail.First.Value))
                {
                    if (finalPosition.X == cell.X && finalPosition.Y == cell.Y)
                    {
                        IsDestroyed = true;
                        return;
                    }
                }
            }
        }

        Trail.AddFirst(finalPosition);
        if (Trail.Count > 4)
        {
            Trail.RemoveLast();
        }

        if (IsPlayer)
        {
            movesSinceLastFuelDecrease++;
            if (movesSinceLastFuelDecrease >= 5)
            {
                Fuel -= 1; // Reduce fuel by 1 every 5 moves
                movesSinceLastFuelDecrease = 0;
            }
        }
        else
        {
            Fuel = 100; // Enemies have infinite fuel
        }
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
    public List<Motorcycle> Motorcycles { get; private set; }

    public Map(int width, int height)
    {
        this.Width = width;
        this.Height = height;
        Grid = new LinkedList<Cell>();
        PowersOnMap = new List<Power>();
        Motorcycles = new List<Motorcycle>();

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
            Power.PowerType randomPowerType = (Power.PowerType)rnd.Next(3); // Random power type
            PowersOnMap.Add(new Power(randomPowerType) { Location = new Cell { X = x, Y = y } });
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
    public enum PowerType { Fuel, Shield, HyperSpeed }
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
            case PowerType.Fuel:
                motorcycle.Fuel = Math.Min(100, motorcycle.Fuel + 20);
                break;
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
        map.Motorcycles.AddRange(motorcycles);
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
            }
        }
    }

    public void Draw(Graphics g)
    {
        foreach (var cell in map.Grid)
        {
            g.FillRectangle(Brushes.Green, cell.X * 20, cell.Y * 20, 20, 20);
        }

        // Draw the boundaries of the map in black
        Pen boundaryPen = new Pen(Color.Black, 5);
        g.DrawRectangle(boundaryPen, 0, 0, map.Width * 20, map.Height * 20);

        foreach (var motorcycle in motorcycles)
        {
            Brush brush = motorcycle.IsPlayer ? Brushes.Blue : Brushes.Red;
            foreach (var cell in motorcycle.Trail)
            {
                g.FillRectangle(brush, cell.X * 20, cell.Y * 20, 20, 20);
            }
        }

        foreach (var power in map.PowersOnMap)
        {
            g.FillRectangle(Brushes.Yellow, power.Location.X * 20, power.Location.Y * 20, 20, 20);
        }
    }
}

public class GameForm : Form
{
    private Game game;

    public GameForm()
    {
        this.Width = 800;
        this.Height = 600;
        game = new Game(40, 30, 4); // Example dimensions and player count
        this.DoubleBuffered = true;
        this.Paint += new PaintEventHandler(GameForm_Paint);
        this.KeyDown += new KeyEventHandler(GameForm_KeyDown);
        Timer timer = new Timer();
        timer.Interval = 100;
        timer.Tick += new EventHandler(GameLoop);
        timer.Start();
    }

    private void GameForm_Paint(object sender, PaintEventArgs e)
    {
        game.Draw(e.Graphics);
    }

    private void GameForm_KeyDown(object sender, KeyEventArgs e)
    {
        var player = game.GetPlayerMotorcycle();
        switch (e.KeyCode)
        {
            case Keys.Up:
                player.CurrentDirection = Direction.Up;
                break;
            case Keys.Down:
                player.CurrentDirection = Direction.Down;
                break;
            case Keys.Left:
                player.CurrentDirection = Direction.Left;
                break;
            case Keys.Right:
                player.CurrentDirection = Direction.Right;
                break;
            case Keys.Space:
                player.UsePower();
                break;
        }
    }

    private void GameLoop(object sender, EventArgs e)
    {
        game.Update();
        this.Invalidate();
    }

    [STAThread]
    static void Main()
    {
        Application.Run(new GameForm());
    }
}
