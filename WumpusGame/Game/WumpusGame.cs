using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WumpusAdventure.Models;

namespace WumpusAdventure.Game
{
    public class WumpusGame
    {
        private GameState State { get; set; }

        public WumpusGame()
        {
            State = new GameState();
        }

        public async Task Start()
        {
            DisplayWelcome();

            Console.Write("Would you like to (1) Start a new game or (2) Load a saved game? ");
            string choice = Console.ReadLine()?.Trim() ?? "1";

            if (choice == "2")
            {
                if (!State.LoadGame())
                {
                    Console.WriteLine("No saved game found or error loading game. Starting a new game.");
                    StartNewGame();
                }
            }
            else
            {
                StartNewGame();
            }

            await GameLoop();
        }

        private void StartNewGame()
        {
            Console.Write("Enter your name, brave adventurer: ");
            string playerName = Console.ReadLine()?.Trim() ?? "";
            if (string.IsNullOrEmpty(playerName))
            {
                playerName = "Adventurer";
            }

            State = new GameState();
            State.InitializeNewGame(playerName);
            Console.WriteLine($"\nWelcome, {playerName}! Your adventure begins...");
        }

        private void DisplayWelcome()
        {
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("                  HUNT THE WUMPUS");
            Console.WriteLine(new string('=', 60));
            Console.WriteLine("\nYou are a brave adventurer exploring a mysterious cave system.");
            Console.WriteLine("Somewhere in these caves lurks the terrible Wumpus!");
            Console.WriteLine("Your goal is to find and shoot the Wumpus with your arrows.");
            Console.WriteLine("\nBut beware! The caves also contain:");
            Console.WriteLine("- Bottomless Pits: Fall in and it's game over");
            Console.WriteLine("- Giant Bats: They'll carry you to a random room");
            Console.WriteLine("\nYou begin with 3 arrows. Use them wisely!");
            Console.WriteLine("\nYou're accompanied by 'Jarvis', an ancient spirit who");
            Console.WriteLine("knows these caves well and may offer advice.");
            Console.WriteLine(new string('=', 60) + "\n");
        }

        private async Task GameLoop()
        {
            while (!State.GameOver)
            {
                if (State.Player?.CurrentRoomId == null || !State.Map.ContainsKey(State.Player.CurrentRoomId))
                {
                    Console.WriteLine("Error: Player position is invalid. Starting a new game.");
                    StartNewGame();
                    continue;
                }

                Room currentRoom = State.Map[State.Player.CurrentRoomId];
                DisplayRoomInfo(currentRoom);

                if (CheckHazards(currentRoom))
                {
                    continue; 
                }

                DisplayWarnings(currentRoom);

                Console.Write("\nWhat would you like to do? ");
                string? command = Console.ReadLine()?.Trim().ToLower() ?? "";
                await ProcessCommand(command);

                State.Turns++;
            }
        }

        private void DisplayRoomInfo(Room room)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"\n------- {room.Name} -------");
            Console.WriteLine(room.Description);
            Console.ResetColor();

            Console.WriteLine("\nExits:");
            foreach (var direction in room.Connections)
            {
                if (direction.Key != null && direction.Value != null && State.Map.ContainsKey(direction.Value))
                {
                    Console.WriteLine($"- {char.ToUpper(direction.Key[0]) + direction.Key.Substring(1)}: {State.Map[direction.Value].Name}");
                }
            }

            if (State.Player?.Inventory != null && State.Player.Inventory.TryGetValue("arrows", out int arrowCount))
            {
                Console.WriteLine($"\nInventory: {arrowCount} arrows");
            }
            else
            {
                Console.WriteLine("\nInventory: 0 arrows");
            }
        }

        private bool CheckHazards(Room room)
        {
            if (room.HasWumpus)
            {
                Console.WriteLine("\nOh no! You've walked right into the Wumpus's den!");
                Console.WriteLine("With a terrifying roar, the Wumpus devours you whole!");
                State.GameOver = true;
                State.Win = false;
                EndGame();
                return true;
            }

            if (room.HasPit)
            {
                Console.WriteLine("\nYou step forward and feel nothing beneath your foot...");
                Console.WriteLine("You've fallen into a bottomless pit!");
                Console.WriteLine("Your screams echo as you fall forever...");
                State.GameOver = true;
                State.Win = false;
                EndGame();
                return true;
            }

            if (room.HasBats)
            {
                Console.WriteLine("\nSuddenly, giant bats swoop down from the ceiling!");
                Console.WriteLine("They grab you and carry you away...");

                List<string> availableRooms = new List<string>(State.Map.Keys);
                Random random = new Random();
                string newRoomId = availableRooms[random.Next(availableRooms.Count)];
                if (State.Player != null)
                {
                    State.Player.CurrentRoomId = newRoomId;
                }

                Console.WriteLine("After a disorienting flight, the bats drop you in a new location.");
                return true;
            }

            return false;
        }

        private void DisplayWarnings(Room currentRoom)
        {
            List<Room> nearbyRooms = new List<Room>();
            foreach (var roomId in currentRoom.Connections.Values)
            {
                if (State.Map.ContainsKey(roomId))
                {
                    nearbyRooms.Add(State.Map[roomId]);
                }
            }

            List<string> warnings = new List<string>();
            if (nearbyRooms.Exists(room => room.HasWumpus))
            {
                warnings.Add("You detect a foul stench... The Wumpus must be nearby!");
            }

            if (nearbyRooms.Exists(room => room.HasPit))
            {
                warnings.Add("You feel a cold draft... There must be a pit nearby.");
            }

            if (nearbyRooms.Exists(room => room.HasBats))
            {
                warnings.Add("You hear the fluttering of wings... Giant bats are nearby!");
            }

            if (warnings.Count > 0)
            {
                Console.WriteLine("\nWarnings:");
                foreach (var warning in warnings)
                {
                    Console.WriteLine($"- {warning}");
                }
            }
        }

        private async Task ProcessCommand(string command)
        {
            if (command == "q" || command == "quit" || command == "exit")
            {
                QuitGame();
                return;
            }

            if (command == "save")
            {
                State.SaveGame();
                Console.WriteLine("Game saved successfully!");
                return;
            }

            if (command == "n" || command == "north" || command == "go north")
            {
                MovePlayer("north");
            }
            else if (command == "s" || command == "south" || command == "go south")
            {
                MovePlayer("south");
            }
            else if (command == "e" || command == "east" || command == "go east")
            {
                MovePlayer("east");
            }
            else if (command == "w" || command == "west" || command == "go west")
            {
                MovePlayer("west");
            }

            else if (command.StartsWith("shoot "))
            {
                string[] parts = command.Split(' ');
                if (parts.Length > 1)
                {
                    string direction = parts[1].ToLower();
                    ShootArrow(direction);
                }
                else
                {
                    Console.WriteLine("Please specify a direction to shoot (e.g., 'shoot north').");
                }
            }

            else if (command == "advice" || command == "help" || command == "jarvis")
            {
                if (State.AICompanion != null)
                {
                    string advice = await State.AICompanion.GetAdvice(State);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\nJarvis says: \"{advice}\"");
                    Console.ResetColor(); 
                }
                else
                {
                    Console.WriteLine("\nYour AI companion isn't available right now.");
                }
            }

            else if (command == "inventory" || command == "i")
            {
                if (State.Player?.Inventory != null && State.Player.Inventory.TryGetValue("arrows", out int arrowCount))
                {
                    Console.WriteLine($"\nYou have {arrowCount} arrows remaining.");
                }
                else
                {
                    Console.WriteLine("\nYou have 0 arrows remaining.");
                }
            }

            else if (command == "commands" || command == "?")
            {
                DisplayCommands();
            }

            else
            {
                Console.WriteLine("I don't understand that command. Type 'commands' for help.");
            }
        }

        private void MovePlayer(string direction)
        {
            if (State.Player == null)
                return;

            Room? currentRoom = State.Player.CurrentRoomId != null && State.Map.ContainsKey(State.Player.CurrentRoomId)
                ? State.Map[State.Player.CurrentRoomId]
                : null;

            if (currentRoom == null)
                return;

            if (currentRoom.Connections.TryGetValue(direction, out string? roomId) && roomId != null)
            {
                State.Player.CurrentRoomId = roomId;
                Console.WriteLine($"You move {direction}.");
            }
            else
            {
                Console.WriteLine($"You can't go {direction} from here.");
            }
        }

        private void ShootArrow(string direction)
        {
            if (State.Player?.Inventory == null ||
                !State.Player.Inventory.TryGetValue("arrows", out int arrowCount) ||
                arrowCount <= 0)
            {
                Console.WriteLine("You're out of arrows!");
                return;
            }

            Room? currentRoom = State.Player.CurrentRoomId != null && State.Map.ContainsKey(State.Player.CurrentRoomId)
                ? State.Map[State.Player.CurrentRoomId]
                : null;

            if (currentRoom == null)
                return;

            if (!currentRoom.Connections.TryGetValue(direction, out string? targetRoomId) || targetRoomId == null)
            {
                Console.WriteLine($"You can't shoot {direction} from here.");
                return;
            }

            State.Player.Inventory["arrows"]--;
            Room? targetRoom = State.Map.ContainsKey(targetRoomId) ? State.Map[targetRoomId] : null;

            Console.WriteLine($"You shoot an arrow {direction}...");
            System.Threading.Thread.Sleep(1000); 

            if (targetRoom != null && targetRoom.HasWumpus)
            {
                Console.WriteLine("\nA horrible shriek echoes through the caves!");
                Console.WriteLine("You've slain the Wumpus! Victory is yours!");
                State.GameOver = true;
                State.Win = true;
                EndGame();
            }
            else
            {
                Console.WriteLine("Your arrow flies into the darkness... and hits nothing.");

                if (State.Player.Inventory["arrows"] <= 0)
                {
                    Console.WriteLine("\nYou've used your last arrow...");
                    Console.WriteLine("With no way to defend yourself, your quest ends here.");
                    Console.WriteLine("Game Over!");
                    State.GameOver = true;
                    State.Win = false;
                    EndGame();
                }
            }
        }

        private void DisplayCommands()
        {
            Console.WriteLine("\nAvailable Commands:");
            Console.WriteLine("- n, north, go north: Move north");
            Console.WriteLine("- s, south, go south: Move south");
            Console.WriteLine("- e, east, go east: Move east");
            Console.WriteLine("- w, west, go west: Move west");
            Console.WriteLine("- shoot [direction]: Shoot an arrow (e.g., 'shoot north')");
            Console.WriteLine("- advice, jarvis: Ask your AI companion for advice");
            Console.WriteLine("- inventory, i: Check your inventory");
            Console.WriteLine("- save: Save your game");
            Console.WriteLine("- quit, exit, q: Quit the game");
            Console.WriteLine("- commands, ?: Show this help message");
        }

        private void EndGame()
        {
            if (State.Win)
            {
                Console.WriteLine("\nCongratulations! You've completed your quest!");
            }
            else
            {
                Console.WriteLine("\nYour adventure has come to an end...");
            }

            Console.WriteLine($"You survived for {State.Turns} turns.");
            Console.WriteLine("Thanks for playing HUNT THE WUMPUS!");

            Console.Write("\nWould you like to play again? (y/n) ");
            string? playAgain = Console.ReadLine()?.Trim().ToLower() ?? "n";
            if (playAgain == "y")
            {
                State = new GameState();
                StartNewGame();
                Task.Run(() => GameLoop()).Wait();
            }
        }

        private void QuitGame()
        {
            Console.Write("Would you like to save your game before quitting? (y/n) ");
            string? save = Console.ReadLine()?.Trim().ToLower() ?? "n";
            if (save == "y")
            {
                State.SaveGame();
                Console.WriteLine("Game saved successfully!");
            }

            Console.WriteLine("\nThanks for playing HUNT THE WUMPUS!");
            State.GameOver = true;
        }
    }
}