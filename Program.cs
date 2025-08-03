using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Text;

class Program
{
    // Игровая карта
    static string map =
        "################//################################" +
        "#..#.............................#...............#" +
        "#..#.............................#...............#" +
        "#..#.............................#..###########..#" +
        "#..#.............................#..#.........#..#" +
        "#...................................#*........#..#" +
        "#...................................#.........[..#" +
        "#..#.............................#..#.........#..#" +
        "#..#.............................#..#.........#..#" +
        "#..#.............................#..###########..#" +
        "#..#.............................#..###########..#" +
        "#..###############################..#.........#..#" +
        "#...................................#.........#..#" +
        "#...................................[.........#..#" +
        "#..############..####[############..#.........#..#" +
        "#..############..#........########..#.........#..#" +
        "#..############..#........########..#.........#..#" +
        "#..############..#*.......########..#.........#..#" +
        "/..############..#........########..###########..#" +
        "/..############..#........########..###########..#" +
        "#..############..#################..#.........#..#" +
        "#..############...........#......#..#.........#..#" +
        "#..############...........#......#..#.........#..#" +
        "#..#####################..#......#..#.........#..#" +
        "#..#...................#..#......#..#*........#../" +
        "#..#...................#..#......#..#.........[../" +
        "#..#...................#..#......[..#.........#..#" +
        "#..[...................#..#......#..#.........#..#" +
        "#..#...................#..#......#..#.........#..#" +
        "#..#...................#..#......#..#.........#..#" +
        "#..#####################..########..###########..#" +
        "#......................................#......#..#" +
        "#......................................#......#..#" +
        "#..#################[###..####[######..#......#..#" +
        "#..###############.....#..#.........#..#......[..#" +
        "#..###############.....#..#.........#..#......#..#" +
        "#..###############.....#..#........*#..#......#..#" +
        "#..###############.....#..#.........#..#......#..#" +
        "#..#.............#.....#..###########..########..#" +
        "#..#.............[.....#..###########..########..#" +
        "#..#.............#######..###########..########..#" +
        "#..#.............#.....#..#.........#..#......#..#" +
        "#..#.............#*....#..#........*#..[......#..#" +
        "#..#.............#.....#..#.........#..#......#..#" +
        "#..#.............#.....[..[.........#..########..#" +
        "#..#.............#.....#..#.........#..########..#" +
        "#..#######[#############..###########..########..#" +
        "#................................................#" +
        "#................................................#" +
        "########################//########################";

    // Игровые переменные
    static int mapsize;
    static int width = 80, height = 25;
    static double playerX = 24.5, playerZ = 1.5;
    static double cameraY = 0, cameraUP = 0;
    static double sensitivity = 0.12;
    static double playerSpeed = 0.125, playerSpeedRun = 0.25;
    static double viewDistance = 60, fov = 90 * 0.0175;
    static int fps = 0, frames = 0, maxFps = 60;
    static int exitsReached = 0, exitsTotal;
    static int pickedupCoins = 0, countOfCoins;
    static double jmph = 0;
    static bool showCoinVar = false;
    static Stopwatch fpsWatch = new Stopwatch();

    // Фреймбуффер
    static char[] frameBuffer =
        // Заполняем пустыми символами, в конце строки - \n
        Enumerable.Range(0, (width + 1) * height).Select(_ => (_ % (width + 1)) == (0) ? '\n' : ' ').ToArray();

    static void Main()
    {
        // Очистка экрана
        Console.Clear();
        mapsize = (int)Math.Sqrt(map.Length);
        map = string.Join("", map.Chunk(mapsize).Reverse().Select(o => new string(o)));

        // Подсчет элементов на карте
        exitsTotal = map.Count(c => c == '/');
        countOfCoins = map.Count(c => c == '*');

        // Настройка консоли
        Console.SetWindowSize(width, height + 1);
        Console.CursorVisible = false;

        StartGame();
    }

    static void StartGame()
    {
        fpsWatch.Start();
        char[] hitChars = { '#', '/', '*', '[' };
        
        while (true)
        {
            // Обновление игрового состояния
            HandleMovement();
            
            // Отрисовка
            Render();

            // Ожидание кадра
            if (maxFps > 0 && (frames+1) * 1000f / maxFps > fpsWatch.ElapsedMilliseconds)
                Thread.Sleep((int)((frames+1) * 1000f / maxFps - fpsWatch.ElapsedMilliseconds));

            // Счетчик FPS
            frames++;
            if (fpsWatch.ElapsedMilliseconds >= 1000)
            {
                fps = frames;
                frames = 0;
                fpsWatch.Restart();
            }
        }
    }

    static void HandleKeyPress(ConsoleKey key)
    {
        switch (key)
        {
            case ConsoleKey.E:
                HandleDoorInteraction();
                break;
        }
    }

    static void HandleDoorInteraction()
    {
        double sin = Math.Sin(cameraY);
        double cos = Math.Cos(cameraY);
        double x = playerX, y = playerZ;
        double dist = 0;
        
        while (dist < 3 && x < mapsize && x > -1 && y < mapsize && y > -1)
        {
            x = playerX + sin * dist;
            y = playerZ + cos * dist;
            dist += 0.5;
            
            int index = (int)(x + 0.25) + (int)(y + 0.25) * mapsize;
            if (map[index] == '[')
            {
                map = new StringBuilder(map) { [index] = '(' }.ToString();
                break;
            }
            else if (map[index] == '(')
            {
                map = new StringBuilder(map) { [index] = '[' }.ToString();
                break;
            }
        }
    }

    static void HandleMovement()
    {
        double newX = playerX, newZ = playerZ;
        bool moved = false;

        if (Console.KeyAvailable)
        {
            var key = Console.ReadKey(true);

            // Выбор скорости в зависимости от состояния Shift
            var speed = key.Modifiers.HasFlag(ConsoleModifiers.Shift) ? playerSpeedRun : playerSpeed;

            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    newX += Math.Sin(cameraY) * speed;
                    newZ += Math.Cos(cameraY) * speed;
                    moved = true;
                    break;
                case ConsoleKey.DownArrow:
                    newX -= Math.Sin(cameraY) * speed;
                    newZ -= Math.Cos(cameraY) * speed;
                    moved = true;
                    break;
                case ConsoleKey.RightArrow:
                    newX += Math.Sin(cameraY + Math.PI / 2) * speed;
                    newZ += Math.Cos(cameraY + Math.PI / 2) * speed;
                    moved = true;
                    break;
                case ConsoleKey.LeftArrow:
                    newX -= Math.Sin(cameraY + Math.PI / 2) * speed;
                    newZ -= Math.Cos(cameraY + Math.PI / 2) * speed;
                    moved = true;
                    break;
                case ConsoleKey.PageDown:
                    cameraY += sensitivity;
                    break;
                case ConsoleKey.Delete:
                    cameraY -= sensitivity;
                    break;

                default:
                    HandleKeyPress(key.Key);
                    break;
            }
        }

        if (moved)
        {
            // Проверка коллизий
            int index = (int)(newX + 0.25) + (int)(newZ + 0.25) * mapsize;
            char posChar = map[index];

            if (posChar == '#' || posChar == '[')
            {
                // Столкновение со стеной - движение отменяется
                return;
            }
            else if (posChar == '*')
            {
                // Сбор монеты
                map = new StringBuilder(map) { [index] = '.' }.ToString();
                pickedupCoins++;
                if (pickedupCoins == countOfCoins)
                    Console.ForegroundColor = ConsoleColor.Red;
            }
            else if (posChar == '/' && pickedupCoins == countOfCoins)
            {
                // Прохождение выхода
                map = new StringBuilder(map) { [index] = '.' }.ToString();
                exitsReached++;
                if (exitsReached == exitsTotal)
                    Environment.Exit(0);
            }
            else
            {
                // Движение разрешено
                playerX = newX;
                playerZ = newZ;
            }
        }
    }

    static void Render()
    {
        Console.SetCursorPosition(0, 0);
        Console.Write($"FPS: {fps} ");

        if (pickedupCoins != countOfCoins)
            Console.Write($"Coins: {pickedupCoins}/{countOfCoins}");
        else
            Console.Write($"Exits: {exitsReached}/{exitsTotal}");

        // Рендеринг 3D сцены
        for (int w = 0; w < width; w++)
        {
            double angle = cameraY - fov / 2 + ((double)w / width * fov);
            RenderColumn(angle, w);
        }

        // Вывод фреймбуффера
        Console.SetCursorPosition(0, 1);
        Console.Write(frameBuffer);
    }

    static void RenderColumn(double angle, int column)
    {
        double sin = Math.Sin(angle);
        double cos = Math.Cos(angle);
        double hitX = playerX, hitY = playerZ;
        double dist = 0;
        bool hit = false;
        char hitChar = '#';
        bool drawDoor = false;

        // Луч столкновения
        while (dist < viewDistance && hitX < mapsize && hitX > -1 && hitY < mapsize && hitY > -1)
        {
            hitX = playerX + dist * sin;
            hitY = playerZ + dist * cos;
            dist += 0.05;
            
            int index = (int)(hitX + 0.25) + (int)(hitY + 0.25) * mapsize;
            if (map[index] == '(') drawDoor = true;
            if ("#/*[".Contains(map[index]))
            {
                hit = true;
                hitChar = map[index];
                break;
            }
        }

        if (hit)
        {
            dist *= Math.Cos(angle - cameraY);
            int ceiling = (int)(height / 2 - height / dist) - (int)(cameraUP * 2 * height / 2) + (int)jmph;
            int floor = height - ceiling;

            ceiling = Math.Clamp(ceiling, 0, height - 1);
            floor = Math.Clamp(floor, 0, height - 1);

            // Отрисовка столбца
            for (int i = 0; i < height; i++)
            {
                if (i > ceiling && i < floor)
                {
                    char c = hitChar switch
                    {
                        '/' => '!',
                        '[' => '[',
                        '*' => showCoinVar ? '@' : '*',
                        _ => dist > 5 ? '░' : dist > 3 ? '▒' : dist > 1 ? '▓' : '█'
                    };
                    
                    if (drawDoor && (i + (int)(height / 2 - height / dist)) % 3 == 1)
                        c = '~';
                    

                    frameBuffer[column + i * (width + 1)+1] = c;
                }
                else if (i >= floor) // Пол
                    frameBuffer[column + i * (width + 1)+1] = '.';
                else // Потолок
                    frameBuffer[column + i * (width + 1)+1] = ' ';
                
            }
        }
    }
}