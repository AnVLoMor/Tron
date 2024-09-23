using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Linq;

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
    public int Fuel { get; set; }
    public Queue<Item> Items { get; private set; }
    public Stack<Power> Powers { get; private set; }
    public bool IsDestroyed { get; set; }
    public Direction CurrentDirection { get; set; }
    public bool IsPlayer { get; set; }
    private int movesSinceLastFuelDecrease;
    private int maxTrailLength = 10;
    public bool IsVisible { get; private set; }
    private int destructionTimer;
    public int BaseSpeed { get; set; }
    public int CurrentSpeed { get; private set; }

    public Motorcycle(int startX, int startY, bool isPlayer)
    {
        Fuel = 100;
        Trail = new LinkedList<Cell>();
        Items = new Queue<Item>();
        Powers = new Stack<Power>();
        IsDestroyed = false;
        CurrentDirection = (Direction)new Random().Next(4);
        IsPlayer = isPlayer;
        movesSinceLastFuelDecrease = 0;
        IsVisible = true;
        destructionTimer = 0;
        BaseSpeed = new Random().Next(1, 2);
        CurrentSpeed = BaseSpeed;

        // Inicializar la motocicleta y su estela con 11 elementos (1 moto + 10 de estela)
        Trail.AddLast(new Cell { X = startX, Y = startY }); // Moto
        for (int i = 1; i <= 3; i++)
        {
            Trail.AddLast(new Cell { X = startX, Y = startY + i }); // Estela
        }
    }
    public void Update(int elapsedMilliseconds)
    {
        if (IsDestroyed && IsVisible)
        {
            destructionTimer += elapsedMilliseconds;
            if (destructionTimer >= 2000) // 2 segundos
            {
                IsVisible = false;
            }
        }
    }
    public void AddPower(Power power)
    {
        Powers.Push(power);
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
            Power power = Powers.Pop();
            power.Activate(this);
        }
    }
    public void RotatePowers()
    {
        if (Powers.Count > 1)
        {
            Power topPower = Powers.Pop();
            Power[] remainingPowers = Powers.ToArray();
            Powers.Clear();
            Powers.Push(topPower);
            foreach (Power power in remainingPowers.Reverse())
            {
                Powers.Push(power);
            }
        }
    }

    public void ResetSpeed()
    {
        CurrentSpeed = BaseSpeed;
    }
}

public class Map
{
    public LinkedList<Cell> Grid { get; private set; }
    public List<Power> PowersOnMap { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public List<Motorcycle> Motorcycles { get; set; }
    private Random rnd = new Random();

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

    public void SpawnRandomPower()
    {
        int x = rnd.Next(0, Width);
        int y = rnd.Next(0, Height);
        Power.PowerType randomPowerType = (Power.PowerType)rnd.Next(4);
        PowersOnMap.Add(new Power(randomPowerType) { Location = new Cell { X = x, Y = y } });
    }
    public void SpawnItemsAndPowers()
    {
        Random rnd = new Random();

        // Generate random powers in random positions
        for (int i = 0; i < 10; i++)
        {
            int x = rnd.Next(0, Width);
            int y = rnd.Next(0, Height);
            Power.PowerType randomPowerType = (Power.PowerType)rnd.Next(4); // Random power type
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
    public bool IsCollidingWithTrail(Cell position)
    {
        foreach (var motorcycle in Motorcycles)
        {
            if (motorcycle.IsDestroyed) continue;

            foreach (var cell in motorcycle.Trail)
            {
                if (position.X == cell.X && position.Y == cell.Y)
                {
                    if (cell == motorcycle.Trail.First.Value)
                    {
                        continue;
                    }
                    return true;
                }
            }
        }
        return false;
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
    public enum PowerType { Fuel, Shield, HyperSpeed, Bomb }
    public PowerType Type { get; set; }
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
                // Implementar lógica para hacer la motocicleta invencible
                break;
            case PowerType.HyperSpeed:
                motorcycle.BaseSpeed += 1;
                break;
            case PowerType.Bomb:
                // Implementar lógica para la bomba
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
    private int powerSpawnCounter;
    private const int PowerSpawnInterval = 5000; // 5 segundos en milisegundos
    private const int InfoPanelWidth = 170;
    private const int InfoPanelHeight = 150;

    public Game(int width, int height, int numPlayers, int formWidth, int formHeight)
    {
        map = new Map(width, height);
        motorcycles = new List<Motorcycle>();

        playerMotorcycle = new Motorcycle(5, 5, true);
        motorcycles.Add(playerMotorcycle);

        offsetX = (formWidth - (width * cellSize)) / 2;
        offsetY = (formHeight - (height * cellSize)) / 2;

        Random rnd = new Random();
        for (int i = 1; i < numPlayers; i++)
        {
            int x = rnd.Next(0, width);
            int y = rnd.Next(0, height);
            motorcycles.Add(new Motorcycle(x, y, false));
        }

        map.Motorcycles.AddRange(motorcycles);
        powerSpawnCounter = 0;
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

    public void Update(int elapsedMilliseconds)
    {
        for (int i = motorcycles.Count - 1; i >= 0; i--)
        {
            var motorcycle = motorcycles[i];
            
            if (motorcycle.IsDestroyed)
            {
                if (!motorcycle.IsPlayer)
                {
                    motorcycles.RemoveAt(i);
                    continue;
                }
            }
            else
            {
                for (int j = 0; j < motorcycle.CurrentSpeed; j++)
                {
                    motorcycle.Move(map);

                    var collidedPower = map.CheckPowerCollision(motorcycle.Trail.First.Value);
                    if (collidedPower != null)
                    {
                        motorcycle.AddPower(collidedPower);
                    }
                }
            }
        }

        // Actualizar la lista de motos en el mapa
        map.Motorcycles = motorcycles;

        // Actualizar el contador y generar un nuevo poder cada 5 segundos
        powerSpawnCounter += elapsedMilliseconds;
        if (powerSpawnCounter >= PowerSpawnInterval)
        {
            map.SpawnRandomPower();
            powerSpawnCounter = 0;
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

        // Dibuja las motocicletas y sus estelas
        foreach (var motorcycle in motorcycles)
        {
            if (motorcycle.IsVisible)
            {
                Brush brush = motorcycle.IsPlayer ? new SolidBrush(Color.FromArgb(173, 216, 230)) : Brushes.Red;
                foreach (var cell in motorcycle.Trail)
                {
                    g.FillRectangle(brush, offsetX + cell.X * cellSize, offsetY + cell.Y * cellSize, cellSize, cellSize);
                }
            }
        }

        // Dibuja los poderes en el mapa
        foreach (var power in map.PowersOnMap)
        {
            Brush powerBrush = GetPowerBrush(power.Type);
            g.FillRectangle(powerBrush, offsetX + power.Location.X * cellSize, offsetY + power.Location.Y * cellSize, cellSize, cellSize);
        }

        // Dibuja el panel de información
        DrawInfoPanel(g);
    }
    private Brush GetPowerBrush(Power.PowerType type)
    {
        switch (type)
        {
            case Power.PowerType.Shield:
                return Brushes.Purple;
            case Power.PowerType.HyperSpeed:
                return Brushes.Yellow;
            case Power.PowerType.Fuel:
                return Brushes.Green;
            case Power.PowerType.Bomb:
                return Brushes.Orange;
            default:
                return Brushes.Gray;
        }
    }
    private void DrawInfoPanel(Graphics g)
    {
        int panelX = offsetX + map.Width * cellSize + 10;
        int panelY = offsetY;

        // Dibuja el fondo del panel
        g.FillRectangle(Brushes.LightGray, panelX, panelY, InfoPanelWidth, InfoPanelHeight);

        // Dibuja el borde del panel
        g.DrawRectangle(Pens.Black, panelX, panelY, InfoPanelWidth, InfoPanelHeight);

        // Muestra la cantidad de combustible
        g.DrawString($"Fuel: {playerMotorcycle.Fuel}", new Font("Arial", 12), Brushes.Black, panelX + 10, panelY + 10);

        // Muestra los poderes guardados
        g.DrawString("Powers:", new Font("Arial", 12), Brushes.Black, panelX + 10, panelY + 40);
        int yOffset = 60;
        foreach (var power in playerMotorcycle.Powers)
        {
            Brush powerBrush = GetPowerBrush(power.Type);
            g.FillRectangle(powerBrush, panelX + 10, panelY + yOffset, 15, 15);
            g.DrawString(power.Type.ToString(), new Font("Arial", 10), Brushes.Black, panelX + 30, panelY + yOffset);
            yOffset += 20;
            
            // Si el panel se llena, detén el dibujo de poderes
            if (yOffset > InfoPanelHeight - 20) break;
        }
    }
    
    // Método para activar el poder del jugador
    public void ActivatePlayerPower()
    {
        playerMotorcycle.UsePower();
    }

    // Método para rotar los poderes del jugador
    public void RotatePlayerPowers()
    {
        playerMotorcycle.RotatePowers();
    }
}

public class GameForm : Form
{
    private Game game;
    private Timer gameTimer;
    private DateTime lastUpdateTime;

    public GameForm()
    {
        this.Width = 1200;
        this.Height = 800;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.Black; // Cambia el color de fondo de la forma
        game = new Game(40, 30, 4, this.ClientSize.Width, this.ClientSize.Height);
        this.DoubleBuffered = true;
        this.Paint += new PaintEventHandler(GameForm_Paint);
        this.KeyDown += new KeyEventHandler(GameForm_KeyDown);
        this.Resize += new EventHandler(GameForm_Resize);
        
        gameTimer = new Timer();
        gameTimer.Interval = 150; // Velocidad del juego
        gameTimer.Tick += new EventHandler(GameLoop);
        gameTimer.Start();

        lastUpdateTime = DateTime.Now;
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
                game.ActivatePlayerPower();
                break;
            case Keys.R:
                game.RotatePlayerPowers();
                break;
        }
    }
    private void GameLoop(object sender, EventArgs e)
    {
        DateTime currentTime = DateTime.Now;
        TimeSpan elapsedTime = currentTime - lastUpdateTime;
        game.Update((int)elapsedTime.TotalMilliseconds);
        lastUpdateTime = currentTime;

        // Resetear la velocidad de las motocicletas después de cada actualización
        foreach (var motorcycle in game.GetMotorcycles())
        {
            motorcycle.ResetSpeed();
        }

        this.Invalidate();
    }

    [STAThread]
    static void Main()
    {
        Application.Run(new GameForm());
    }
}
