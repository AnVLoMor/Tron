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
    private int maxTrailLength = 10;

    public Motorcycle(int startX, int startY, bool isPlayer)
    {
        Speed = new Random().Next(1, 11);
        Fuel = 100;
        Trail = new LinkedList<Cell>();
        Items = new Queue<Item>();
        Powers = new Stack<Power>();
        IsDestroyed = false;
        CurrentDirection = (Direction)new Random().Next(4);
        IsPlayer = isPlayer;
        movesSinceLastFuelDecrease = 0;

        // Inicializar la motocicleta y su estela con 11 elementos (1 moto + 10 de estela)
        Trail.AddLast(new Cell { X = startX, Y = startY }); // Moto
        for (int i = 1; i <= 3; i++)
        {
            Trail.AddLast(new Cell { X = startX, Y = startY + i }); // Estela
        }
    }

    public void Move(Map map)
    {
        if (Fuel <= 0 || IsDestroyed)
        {
            IsDestroyed = true;
            return;
        }

        Cell nextPosition;
        if (!IsPlayer)
        {
            ChooseSafeDirection(map);
        }
        
        nextPosition = GetNextPosition(CurrentDirection);

        // Check if the next position is out of bounds or colliding with any trail
        if (!IsSafePosition(nextPosition, map))
        {
            if (IsPlayer)
            {
                IsDestroyed = true;
                return;
            }
            else
            {
                // If it's an enemy, try to find a safe direction again
                ChooseSafeDirection(map);
                nextPosition = GetNextPosition(CurrentDirection);
                
                // If still not safe, destroy the enemy
                if (!IsSafePosition(nextPosition, map))
                {
                    IsDestroyed = true;
                    return;
                }
            }
        }

        // Move to the next position
        Trail.AddFirst(nextPosition);

        // Manage trail length
        if (Trail.Count > maxTrailLength + 1)
        {
            Trail.RemoveLast();
        }

        // Fuel management
        if (IsPlayer)
        {
            movesSinceLastFuelDecrease++;
            if (movesSinceLastFuelDecrease >= 5)
            {
                Fuel -= 1;
                movesSinceLastFuelDecrease = 0;
            }
        }
        else
        {
            Fuel = 100; // Enemies have infinite fuel
        }
    }

    private void ChooseSafeDirection(Map map)
    {
        List<Direction> safeDirections = new List<Direction>();
        foreach (Direction dir in Enum.GetValues(typeof(Direction)))
        {
            Cell nextPosition = GetNextPosition(dir);
            if (IsSafePosition(nextPosition, map))
            {
                safeDirections.Add(dir);
            }
        }

        if (safeDirections.Count > 0)
        {
            CurrentDirection = safeDirections[new Random().Next(safeDirections.Count)];
        }
        // If no safe directions, keep current direction (will likely lead to destruction in the next move)
    }

    private bool IsSafePosition(Cell position, Map map)
    {
        // Check map boundaries
        if (position.X < 0 || position.X >= map.Width || position.Y < 0 || position.Y >= map.Height)
        {
            return false;
        }

        // Check collision with any trail
        return !IsCollidingWithTrail(position, map);
    }

    private bool IsCollidingWithTrail(Cell position, Map map)
    {
        foreach (var motorcycle in map.Motorcycles)
        {
            foreach (var cell in motorcycle.Trail)
            {
                if (position.X == cell.X && position.Y == cell.Y)
                {
                    // Don't consider the head of this motorcycle as a collision
                    if (motorcycle == this && cell == Trail.First.Value)
                    {
                        continue;
                    }
                    return true;
                }
            }
        }
        return false;
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
    private int cellSize = 20;
    private int offsetX;
    private int offsetY;

    public Game(int width, int height, int numPlayers, int formWidth, int formHeight)
    {
        map = new Map(width, height);
        motorcycles = new List<Motorcycle>();

        // Initialize the player's motorcycle in a fixed position
        playerMotorcycle = new Motorcycle(5, 5, true);
        motorcycles.Add(playerMotorcycle);

        offsetX = (formWidth - (width * cellSize)) / 2;
        offsetY = (formHeight - (height * cellSize)) / 2;

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
        // Dibuja el fondo del mapa
        g.FillRectangle(Brushes.DarkBlue, offsetX, offsetY, map.Width * cellSize, map.Height * cellSize);

        // Dibuja la cuadrícula
        for (int x = 0; x <= map.Width; x++)
        {
            g.DrawLine(Pens.Black, offsetX + x * cellSize, offsetY, offsetX + x * cellSize, offsetY + map.Height * cellSize);
        }
        for (int y = 0; y <= map.Height; y++)
        {
            g.DrawLine(Pens.Black, offsetX, offsetY + y * cellSize, offsetX + map.Width * cellSize, offsetY + y * cellSize);
        }

        // Dibuja los bordes del mapa
        Pen boundaryPen = new Pen(Color.Black, 3);
        g.DrawRectangle(boundaryPen, offsetX, offsetY, map.Width * cellSize, map.Height * cellSize);

        foreach (var motorcycle in motorcycles)
        {
            Brush brush = motorcycle.IsPlayer ? new SolidBrush(Color.FromArgb(173, 216, 230)) : Brushes.Red; // Celeste claro
            foreach (var cell in motorcycle.Trail)
            {
                g.FillRectangle(brush, offsetX + cell.X * cellSize, offsetY + cell.Y * cellSize, cellSize, cellSize);
            }
        }

        foreach (var power in map.PowersOnMap)
        {
            g.FillRectangle(Brushes.Yellow, offsetX + power.Location.X * cellSize, offsetY + power.Location.Y * cellSize, cellSize, cellSize);
        }
    }

}

public class GameForm : Form
{
    private Game game;
    private Timer gameTimer;

    public GameForm()
    {
        this.Width = 1000;
        this.Height = 800;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.Black; // Cambia el color de fondo de la forma
        game = new Game(40, 30, 4, this.ClientSize.Width, this.ClientSize.Height);
        this.DoubleBuffered = true;
        this.Paint += new PaintEventHandler(GameForm_Paint);
        this.KeyDown += new KeyEventHandler(GameForm_KeyDown);
        this.Resize += new EventHandler(GameForm_Resize);
        
        gameTimer = new Timer();
        gameTimer.Interval = 150; // Increased from 100 to 200 ms to slow down the game
        gameTimer.Tick += new EventHandler(GameLoop);
        gameTimer.Start();
    }

    private void GameForm_Paint(object sender, PaintEventArgs e)
    {
        game.Draw(e.Graphics);
    }

    private void GameForm_Resize(object sender, EventArgs e)
    {
        // Recrea el juego con las nuevas dimensiones del formulario
        game = new Game(40, 30, 4, this.ClientSize.Width, this.ClientSize.Height);
        this.Invalidate();
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
