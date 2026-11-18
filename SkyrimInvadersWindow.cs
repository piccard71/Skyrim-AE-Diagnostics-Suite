using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SkyrimAutoDiag
{
    public class SkyrimInvadersWindow : Window
    {
        private readonly Canvas _gameCanvas;
        private readonly TextBlock _scoreText;
        private readonly TextBlock _livesText;
        private readonly TextBlock _waveText;
        private readonly Border _overlayBorder;
        private readonly TextBlock _overlayTitle;
        private readonly TextBlock _overlaySub;
        private readonly DispatcherTimer _timer;

        private const double CanvasWidth = 680;
        private const double CanvasHeight = 520;

        private FrameworkElement _player = null!;
        private double _playerX = CanvasWidth / 2 - 24;
        private bool _moveLeft, _moveRight, _spacePressed;
        private int _shootCooldown = 0;

        private int _score = 0;
        private int _lives = 3;
        private int _wave = 1;
        private bool _isGameOver = false;

        private double _enemyDir = 1;
        private double _enemySpeed = 1.3;
        private readonly Random _rng = new Random();

        private class Enemy
        {
            public FrameworkElement Visual = null!;
            public double X, Y;
            public int Points;
            public bool Alive = true;
        }

        private class Bullet
        {
            public FrameworkElement Visual = null!;
            public double X, Y, SpeedY;
        }

        private class BunkerBlock
        {
            public Rectangle Visual = null!;
            public double X, Y;
            public int Hp = 3;
        }

        private readonly List<Enemy> _enemies = new();
        private readonly List<Bullet> _playerBullets = new();
        private readonly List<Bullet> _enemyBullets = new();
        private readonly List<BunkerBlock> _bunkers = new();

        public SkyrimInvadersWindow()
        {
            Title = "Skyrim AE - CTD Invaders (Mini-Game)";
            Width = 730;
            Height = 640;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#181A1F"));
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E6E6E6"));

            Grid root = new Grid { Margin = new Thickness(14) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // 1. HEADER IM PROGRAMM-DESIGN
            Border header = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#21252B")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(0, 0, 0, 10)
            };

            DockPanel headerDock = new DockPanel { LastChildFill = false };
            TextBlock titleTxt = new TextBlock
            {
                Text = "⚔️ SKYRIM INVADERS: DOVAHKIIN VS. ALDUIN'S HORDE",
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#61AFEF")),
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(titleTxt, Dock.Left);
            headerDock.Children.Add(titleTxt);

            StackPanel statsPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            _waveText = new TextBlock { Text = "Welle: 1", FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#98C379")), Margin = new Thickness(0, 0, 16, 0) };
            _scoreText = new TextBlock { Text = "Drachenseelen: 0", FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5C07B")), Margin = new Thickness(0, 0, 16, 0) };
            _livesText = new TextBlock { Text = "❤️ ❤️ ❤️", FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E06C75")) };

            statsPanel.Children.Add(_waveText);
            statsPanel.Children.Add(_scoreText);
            statsPanel.Children.Add(_livesText);
            DockPanel.SetDock(statsPanel, Dock.Right);
            headerDock.Children.Add(statsPanel);
            header.Child = headerDock;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // 2. SPIELFELD (CANVAS)
            Border arenaBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0E1015")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3E4452")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                ClipToBounds = true
            };

            Grid arenaGrid = new Grid();
            _gameCanvas = new Canvas { Width = CanvasWidth, Height = CanvasHeight, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            arenaGrid.Children.Add(_gameCanvas);

            // Game Over / Sieg Overlay
            _overlayBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(225, 24, 26, 31)),
                Visibility = Visibility.Collapsed
            };
            StackPanel overlayStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            _overlayTitle = new TextBlock { FontSize = 26, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E06C75")), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 8) };
            _overlaySub = new TextBlock { FontSize = 14, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5C07B")), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 18) };
            Button restartBtn = new Button
            {
                Content = "🔄 Neues Spiel starten (Enter)",
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2EA043")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(18, 9, 18, 9),
                Cursor = Cursors.Hand
            };
            restartBtn.Click += (_, _) => StartNewGame();

            overlayStack.Children.Add(_overlayTitle);
            overlayStack.Children.Add(_overlaySub);
            overlayStack.Children.Add(restartBtn);
            _overlayBorder.Child = overlayStack;
            arenaGrid.Children.Add(_overlayBorder);

            arenaBorder.Child = arenaGrid;
            Grid.SetRow(arenaBorder, 1);
            root.Children.Add(arenaBorder);

            // 3. FOOTER (STEUERUNG)
            Border footer = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#21252B")),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(0, 10, 0, 0)
            };
            footer.Child = new TextBlock
            {
                Text = "Steuerung: [◄ / ►] oder [A / D] = Dovahkiin bewegen   |   [LEERTASTE] = Fus-Ro-Dah Schrei   |   [ESC] = Schließen",
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#98C379")),
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetRow(footer, 2);
            root.Children.Add(footer);

            Content = root;

            KeyDown += OnWindowKeyDown;
            KeyUp += OnWindowKeyUp;
            Closed += (_, _) => _timer?.Stop();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            _timer.Tick += GameLoop;

            StartNewGame();
        }

        private void StartNewGame()
        {
            _score = 0;
            _lives = 3;
            _wave = 1;
            _enemySpeed = 1.3;
            _isGameOver = false;
            _overlayBorder.Visibility = Visibility.Collapsed;
            UpdateHud();
            SetupLevel(resetBunkers: true);
            _timer.Start();
        }

        private void SetupLevel(bool resetBunkers)
        {
            _gameCanvas.Children.Clear();
            _enemies.Clear();
            _playerBullets.Clear();
            _enemyBullets.Clear();

            // Sovngarde-Sternenhimmel & Nordlicht-Schimmer im Hintergrund
            for (int i = 0; i < 40; i++)
            {
                double size = _rng.Next(1, 4);
                Ellipse star = new Ellipse
                {
                    Width = size,
                    Height = size,
                    Fill = new SolidColorBrush(Color.FromArgb((byte)_rng.Next(50, 150), 97, 175, 239))
                };
                Canvas.SetLeft(star, _rng.Next(10, (int)CanvasWidth - 10));
                Canvas.SetTop(star, _rng.Next(10, (int)CanvasHeight - 60));
                _gameCanvas.Children.Add(star);
            }

            // Detaillierter Dovahkiin-Spieler
            _player = CreateDetailedDovahkiinSprite();
            _playerX = CanvasWidth / 2 - 24;
            Canvas.SetLeft(_player, _playerX);
            Canvas.SetTop(_player, CanvasHeight - 52);
            _gameCanvas.Children.Add(_player);

            // Runenstein-Schutzschilde (Bunker)
            if (resetBunkers)
            {
                _bunkers.Clear();
                double[] bunkerCenters = { 135, 340, 545 };
                foreach (double bx in bunkerCenters)
                {
                    for (int r = 0; r < 3; r++)
                    {
                        for (int c = 0; c < 5; c++)
                        {
                            if (r == 2 && (c == 1 || c == 2 || c == 3)) continue;
                            Rectangle block = new Rectangle
                            {
                                Width = 14,
                                Height = 10,
                                Fill = new LinearGradientBrush(
                                    (Color)ColorConverter.ConvertFromString("#3B82F6"),
                                    (Color)ColorConverter.ConvertFromString("#1D4ED8"),
                                    90),
                                Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#61AFEF")),
                                StrokeThickness = 0.6
                            };
                            double x = bx - 35 + c * 14;
                            double y = CanvasHeight - 110 + r * 10;
                            Canvas.SetLeft(block, x);
                            Canvas.SetTop(block, y);
                            _gameCanvas.Children.Add(block);
                            _bunkers.Add(new BunkerBlock { Visual = block, X = x, Y = y, Hp = 3 });
                        }
                    }
                }
            }
            else
            {
                foreach (var b in _bunkers)
                {
                    _gameCanvas.Children.Add(b.Visual);
                }
            }

            // Gegner-Formation spawnen (4 Reihen x 9 Spalten)
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    FrameworkElement vis;
                    int pts;
                    if (row == 0)
                    {
                        vis = CreateDetailedDragonSprite(isAlduin: col % 2 == 0);
                        pts = 35;
                    }
                    else if (row == 1)
                    {
                        vis = CreateDetailedDaedraSprite();
                        pts = 20;
                    }
                    else
                    {
                        vis = CreateDetailedFrostbiteSpiderSprite();
                        pts = 10;
                    }

                    double ex = 62 + col * 62;
                    double ey = 42 + row * 48;
                    Canvas.SetLeft(vis, ex);
                    Canvas.SetTop(vis, ey);
                    _gameCanvas.Children.Add(vis);

                    _enemies.Add(new Enemy { Visual = vis, X = ex, Y = ey, Points = pts });
                }
            }
        }

        private void GameLoop(object? sender, EventArgs e)
        {
            if (_isGameOver) return;

            // 1. Dovahkiin bewegen
            if (_moveLeft && _playerX > 12) _playerX -= 5.4;
            if (_moveRight && _playerX < CanvasWidth - 60) _playerX += 5.4;
            Canvas.SetLeft(_player, _playerX);

            // 2. Schießen (Fus Ro Dah Thu'um)
            if (_shootCooldown > 0) _shootCooldown--;
            if (_spacePressed && _shootCooldown == 0 && _playerBullets.Count < 3)
            {
                FrameworkElement thum = CreateDetailedThuumWave();
                double bx = _playerX + 13;
                double by = CanvasHeight - 66;
                Canvas.SetLeft(thum, bx);
                Canvas.SetTop(thum, by);
                _gameCanvas.Children.Add(thum);
                _playerBullets.Add(new Bullet { Visual = thum, X = bx, Y = by, SpeedY = -8.8 });
                _shootCooldown = 15;
            }

            // 3. Spieler-Schüsse aktualisieren
            for (int i = _playerBullets.Count - 1; i >= 0; i--)
            {
                var b = _playerBullets[i];
                b.Y += b.SpeedY;
                Canvas.SetTop(b.Visual, b.Y);

                if (b.Y < -20)
                {
                    _gameCanvas.Children.Remove(b.Visual);
                    _playerBullets.RemoveAt(i);
                    continue;
                }

                // Treffer auf Bunker prüfen
                var hitBunker = _bunkers.FirstOrDefault(bk => bk.Hp > 0 && Math.Abs(b.X + 11 - (bk.X + 7)) < 13 && Math.Abs(b.Y - (bk.Y + 5)) < 9);
                if (hitBunker != null)
                {
                    DamageBunker(hitBunker);
                    _gameCanvas.Children.Remove(b.Visual);
                    _playerBullets.RemoveAt(i);
                    continue;
                }

                // Treffer auf Gegner prüfen
                var hitEnemy = _enemies.FirstOrDefault(en => en.Alive && b.X + 18 >= en.X && b.X <= en.X + 44 && b.Y <= en.Y + 32 && b.Y + 12 >= en.Y);
                if (hitEnemy != null)
                {
                    hitEnemy.Alive = false;
                    _gameCanvas.Children.Remove(hitEnemy.Visual);
                    _gameCanvas.Children.Remove(b.Visual);
                    _playerBullets.RemoveAt(i);

                    _score += hitEnemy.Points;
                    UpdateHud();

                    if (_enemies.All(en => !en.Alive))
                    {
                        _wave++;
                        _enemySpeed += 0.35;
                        UpdateHud();
                        SetupLevel(resetBunkers: false);
                        return;
                    }
                }
            }

            // 4. Gegner-Formation bewegen
            bool hitWall = false;
            foreach (var en in _enemies.Where(x => x.Alive))
            {
                en.X += _enemyDir * _enemySpeed;
                if (en.X <= 12 || en.X >= CanvasWidth - 58) hitWall = true;
            }

            if (hitWall)
            {
                _enemyDir = -_enemyDir;
                foreach (var en in _enemies.Where(x => x.Alive))
                {
                    en.Y += 16;
                    if (en.Y >= CanvasHeight - 82)
                    {
                        EndGame("ALDUIN HAT GESIEGT (CTD)!", $"Die Drachen haben deine Load-Order verbrannt! Gesammelte Seelen: {_score}");
                        return;
                    }
                }
            }

            foreach (var en in _enemies.Where(x => x.Alive))
            {
                Canvas.SetLeft(en.Visual, en.X);
                Canvas.SetTop(en.Visual, en.Y);
            }

            // 5. Gegner feuern Drachenfeuer / Gift nach unten
            var aliveEnemies = _enemies.Where(x => x.Alive).ToList();
            if (aliveEnemies.Count > 0 && _rng.NextDouble() < 0.03 + (_wave * 0.006))
            {
                var shooter = aliveEnemies[_rng.Next(aliveEnemies.Count)];
                FrameworkElement fireball = CreateDragonFireballVisual();
                double fx = shooter.X + 18;
                double fy = shooter.Y + 28;
                Canvas.SetLeft(fireball, fx);
                Canvas.SetTop(fireball, fy);
                _gameCanvas.Children.Add(fireball);
                _enemyBullets.Add(new Bullet { Visual = fireball, X = fx, Y = fy, SpeedY = 4.7 });
            }

            // 6. Gegner-Schüsse aktualisieren
            for (int i = _enemyBullets.Count - 1; i >= 0; i--)
            {
                var eb = _enemyBullets[i];
                eb.Y += eb.SpeedY;
                Canvas.SetTop(eb.Visual, eb.Y);

                if (eb.Y > CanvasHeight)
                {
                    _gameCanvas.Children.Remove(eb.Visual);
                    _enemyBullets.RemoveAt(i);
                    continue;
                }

                // Treffer auf Bunker
                var hitBunker = _bunkers.FirstOrDefault(bk => bk.Hp > 0 && Math.Abs(eb.X + 5 - (bk.X + 7)) < 11 && Math.Abs(eb.Y - (bk.Y + 5)) < 9);
                if (hitBunker != null)
                {
                    DamageBunker(hitBunker);
                    _gameCanvas.Children.Remove(eb.Visual);
                    _enemyBullets.RemoveAt(i);
                    continue;
                }

                // Treffer auf Dovahkiin
                if (eb.Y >= CanvasHeight - 52 && eb.Y <= CanvasHeight - 14 && eb.X >= _playerX - 4 && eb.X <= _playerX + 48)
                {
                    _gameCanvas.Children.Remove(eb.Visual);
                    _enemyBullets.RemoveAt(i);
                    _lives--;
                    UpdateHud();

                    if (_lives <= 0)
                    {
                        EndGame("CRASH TO DESKTOP (GAME OVER)", $"Du hast {(_wave - 1)} Wellen überlebt und {_score} Drachenseelen absorbiert!");
                        return;
                    }
                }
            }
        }

        private void DamageBunker(BunkerBlock b)
        {
            b.Hp--;
            b.Visual.Opacity = b.Hp * 0.33;
            if (b.Hp <= 0)
            {
                _gameCanvas.Children.Remove(b.Visual);
                _bunkers.Remove(b);
            }
        }

        private void EndGame(string title, string sub)
        {
            _isGameOver = true;
            _timer.Stop();
            _overlayTitle.Text = title;
            _overlaySub.Text = sub;
            _overlayBorder.Visibility = Visibility.Visible;
        }

        private void UpdateHud()
        {
            _waveText.Text = $"Welle: {_wave}";
            _scoreText.Text = $"Drachenseelen: {_score}";
            _livesText.Text = _lives switch
            {
                3 => "❤️ ❤️ ❤️",
                2 => "❤️ ❤️ 🖤",
                1 => "❤️ 🖤 🖤",
                _ => "💀"
            };
        }

        // =========================================================================
        // REALISTISCHE, MEHRSCHICHTIGE SKYRIM-VEKTOR-SPRITES (100% IM CODE GEBAUT)
        // =========================================================================

        /// <summary>
        /// 1. DOVAHKIIN (SPIELER): Detaillierter Nord-Eisenhelm mit geschwungenen Hörnern,
        /// Stahl-Schattierung, Mittelkamm, Nasenschutz, Schulterplatten und leuchtenden Thu'um-Augen.
        /// </summary>
        private static FrameworkElement CreateDetailedDovahkiinSprite()
        {
            Canvas c = new Canvas { Width = 48, Height = 40 };

            // Metall- & Horn-Farbverläufe
            LinearGradientBrush hornBrush = new LinearGradientBrush(
                (Color)ColorConverter.ConvertFromString("#F3E5AB"),
                (Color)ColorConverter.ConvertFromString("#9A7B3E"), 45);

            LinearGradientBrush steelBrush = new LinearGradientBrush(
                (Color)ColorConverter.ConvertFromString("#9CA3AF"),
                (Color)ColorConverter.ConvertFromString("#374151"), 90);

            SolidColorBrush darkIron = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F2937"));
            SolidColorBrush runeCyan = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#61AFEF"));
            SolidColorBrush goldTrim = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5C07B"));

            // Schulterplatten / Nord-Rüstung unten
            Path shoulders = new Path
            {
                Data = Geometry.Parse("M 6,32 Q 24,25 42,32 L 46,39 L 2,39 Z"),
                Fill = darkIron,
                Stroke = goldTrim,
                StrokeThickness = 1
            };

            // Linkes & Rechtes geschwungenes Drachenblut-Horn
            Path leftHorn = new Path
            {
                Data = Geometry.Parse("M 13,20 C 4,21 0,13 3,3 C 5,10 9,14 15,15 Z"),
                Fill = hornBrush,
                Stroke = darkIron,
                StrokeThickness = 1
            };
            Path rightHorn = new Path
            {
                Data = Geometry.Parse("M 35,20 C 44,21 48,13 45,3 C 43,10 39,14 33,15 Z"),
                Fill = hornBrush,
                Stroke = darkIron,
                StrokeThickness = 1
            };

            // Eiserner Helmkessel (Kuppel)
            Path dome = new Path
            {
                Data = Geometry.Parse("M 13,24 C 13,9 35,9 35,24 L 36,33 L 12,33 Z"),
                Fill = steelBrush,
                Stroke = darkIron,
                StrokeThickness = 1.2
            };

            // dunkle Augenhöhlen-Maske + Nasenschutz (Nasal)
            Path visorMask = new Path
            {
                Data = Geometry.Parse("M 14,22 L 34,22 L 34,28 L 26,28 L 24,34 L 22,28 L 14,28 Z"),
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#111318")),
                Stroke = goldTrim,
                StrokeThickness = 0.8
            };

            // Mittelkamm & Stirnband mit Nieten
            Path crest = new Path
            {
                Data = Geometry.Parse("M 22.5,10 L 25.5,10 L 25.5,33 L 22.5,33 Z M 13,21 L 35,21 L 35,23.5 L 13,23.5 Z"),
                Fill = goldTrim
            };

            // Leuchtende Dovahkiin-Augenschlitze (#61AFEF)
            Polygon leftEye = new Polygon
            {
                Points = new PointCollection { new(16, 25), new(21, 25.5), new(20, 27), new(16, 26.5) },
                Fill = runeCyan
            };
            Polygon rightEye = new Polygon
            {
                Points = new PointCollection { new(32, 25), new(27, 25.5), new(28, 27), new(32, 26.5) },
                Fill = runeCyan
            };

            c.Children.Add(shoulders);
            c.Children.Add(leftHorn);
            c.Children.Add(rightHorn);
            c.Children.Add(dome);
            c.Children.Add(visorMask);
            c.Children.Add(crest);
            c.Children.Add(leftEye);
            c.Children.Add(rightEye);

            c.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#61AFEF"),
                BlurRadius = 8,
                ShadowDepth = 0,
                Opacity = 0.55
            };

            return c;
        }

        /// <summary>
        /// 2. SKYRIM-DRACHE (OBERSTE REIHE): Gegliederte Fledermaus-Schwingen mit Flügelschlag-Animation,
        /// Knochenstreben, geschupptem Drachenrumpf, Hörnerkopf, Stachelschweif und glühenden Augen.
        /// </summary>
        private static FrameworkElement CreateDetailedDragonSprite(bool isAlduin)
        {
            Grid dragonGrid = new Grid { Width = 46, Height = 34, RenderTransformOrigin = new Point(0.5, 0.5) };

            Color mainColor = isAlduin
                ? (Color)ColorConverter.ConvertFromString("#E06C75") // Blutdrache / Rot
                : (Color)ColorConverter.ConvertFromString("#D19A66"); // Älterer Drache / Bronze-Gold

            Color wingMembraneColor = isAlduin
                ? (Color)ColorConverter.ConvertFromString("#7F1D1D")
                : (Color)ColorConverter.ConvertFromString("#78350F");

            LinearGradientBrush wingBrush = new LinearGradientBrush(mainColor, wingMembraneColor, 90);
            SolidColorBrush boneStroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5C07B"));
            SolidColorBrush bodyBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27272A"));

            // Flügel-Ebene (wird animiert für echten Flügelschlag!)
            Canvas wingsCanvas = new Canvas { Width = 46, Height = 34, RenderTransformOrigin = new Point(0.5, 0.5) };
            ScaleTransform wingFlap = new ScaleTransform(1.0, 1.0);
            wingsCanvas.RenderTransform = wingFlap;

            // Linke & Rechte gezackte Drachenschwinge mit Flughaut-Bögen
            Path wings = new Path
            {
                Data = Geometry.Parse(
                    "M 21,15 L 11,4 L 1,9 L 4,20 Q 9,16 12,22 Q 16,17 20,21 Z " +
                    "M 25,15 L 35,4 L 45,9 L 42,20 Q 37,16 34,22 Q 30,17 26,21 Z"),
                Fill = wingBrush,
                Stroke = boneStroke,
                StrokeThickness = 1.1
            };

            // Innere Flügelknochen-Streben
            Path wingBones = new Path
            {
                Data = Geometry.Parse("M 20,15 L 11,4 M 18,16 L 12,22 M 26,15 L 35,4 M 28,16 L 34,22"),
                Stroke = boneStroke,
                StrokeThickness = 0.8
            };
            wingsCanvas.Children.Add(wings);
            wingsCanvas.Children.Add(wingBones);

            // Drachenrumpf, Hörnerkopf (unten Richtung Spieler blickend) & Stachelschweif (oben)
            Canvas bodyCanvas = new Canvas { Width = 46, Height = 34 };
            Path bodyAndTail = new Path
            {
                Data = Geometry.Parse("M 23,1 L 25,8 L 27,16 L 25,26 L 23,31 L 21,26 L 19,16 L 21,8 Z"),
                Fill = bodyBrush,
                Stroke = new SolidColorBrush(mainColor),
                StrokeThickness = 1.2
            };

            // Zurückgebogene Drachenhörner am Schädel
            Path horns = new Path
            {
                Data = Geometry.Parse("M 20,24 L 15,19 M 26,24 L 31,19"),
                Stroke = boneStroke,
                StrokeThickness = 1.5
            };

            // Glühende Drachenaugen
            Ellipse eyeL = new Ellipse { Width = 2.5, Height = 2.5, Fill = Brushes.Yellow };
            Canvas.SetLeft(eyeL, 20.2); Canvas.SetTop(eyeL, 25);
            Ellipse eyeR = new Ellipse { Width = 2.5, Height = 2.5, Fill = Brushes.Yellow };
            Canvas.SetLeft(eyeR, 23.5); Canvas.SetTop(eyeR, 25);

            bodyCanvas.Children.Add(bodyAndTail);
            bodyCanvas.Children.Add(horns);
            bodyCanvas.Children.Add(eyeL);
            bodyCanvas.Children.Add(eyeR);

            dragonGrid.Children.Add(wingsCanvas);
            dragonGrid.Children.Add(bodyCanvas);

            // Sanfte Flügelschlag-Animation starten
            DoubleAnimation flapAnim = new DoubleAnimation(1.0, 0.78, TimeSpan.FromMilliseconds(420))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            wingFlap.BeginAnimation(ScaleTransform.ScaleXProperty, flapAnim);

            return dragonGrid;
        }

        /// <summary>
        /// 3. DAEDRA-RUNEN-WÄCHTER (MITTLERE REIHE): Dämonische Oblivion-Rüstung mit gezackten
        /// Schulterdornen, lila Seelenstein-Kern und glühendem Visier.
        /// </summary>
        private static FrameworkElement CreateDetailedDaedraSprite()
        {
            Canvas c = new Canvas { Width = 40, Height = 30 };

            LinearGradientBrush daedricArmor = new LinearGradientBrush(
                (Color)ColorConverter.ConvertFromString("#4C1D95"),
                (Color)ColorConverter.ConvertFromString("#1E1B4B"), 90);
            SolidColorBrush purpleGlow = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C678DD"));
            SolidColorBrush crimson = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E06C75"));

            // Gezackte Daedra-Schulterplatten & Hörnerkrone
            Path armorShape = new Path
            {
                Data = Geometry.Parse("M 20,2 L 25,9 L 33,3 L 38,14 L 31,26 L 24,22 L 20,28 L 16,22 L 9,26 L 2,14 L 7,3 L 15,9 Z"),
                Fill = daedricArmor,
                Stroke = purpleGlow,
                StrokeThickness = 1.2
            };

            // Leuchtender Oblivion-Runenkern auf der Brust
            Polygon soulGem = new Polygon
            {
                Points = new PointCollection { new(20, 10), new(24, 15), new(20, 20), new(16, 15) },
                Fill = crimson,
                Stroke = Brushes.White,
                StrokeThickness = 0.6
            };

            c.Children.Add(armorShape);
            c.Children.Add(soulGem);
            return c;
        }

        /// <summary>
        /// 4. FROSTBISSSPINNE (UNTERE REIHEN): 8 angewinkelte Gliederbeine, Hinterleib (Abdomen)
        /// mit Eismuster, Kopfbruststück, Giftzangen (Cheliceren) und Augencluster.
        /// </summary>
        private static FrameworkElement CreateDetailedFrostbiteSpiderSprite()
        {
            Canvas c = new Canvas { Width = 42, Height = 30 };

            SolidColorBrush legBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6EE7B7"));
            LinearGradientBrush bodyBrush = new LinearGradientBrush(
                (Color)ColorConverter.ConvertFromString("#065F46"),
                (Color)ColorConverter.ConvertFromString("#064E3B"), 90);
            SolidColorBrush iceMark = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#98C379"));

            // 8 angewinkelte Spinnenbeine (4 links, 4 rechts)
            Path legs = new Path
            {
                Data = Geometry.Parse(
                    "M 16,13 L 7,5 L 1,10 " +
                    "M 15,15 L 5,12 L 0,18 " +
                    "M 15,17 L 6,19 L 2,26 " +
                    "M 16,19 L 9,25 L 6,29 " +
                    "M 26,13 L 35,5 L 41,10 " +
                    "M 27,15 L 37,12 L 42,18 " +
                    "M 27,17 L 36,19 L 40,26 " +
                    "M 26,19 L 33,25 L 36,29"),
                Stroke = legBrush,
                StrokeThickness = 1.4,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };

            // Hinterleib (Abdomen)
            Ellipse abdomen = new Ellipse
            {
                Width = 16,
                Height = 14,
                Fill = bodyBrush,
                Stroke = iceMark,
                StrokeThickness = 1
            };
            Canvas.SetLeft(abdomen, 13);
            Canvas.SetTop(abdomen, 2);

            // Kopfbruststück (Cephalothorax)
            Ellipse head = new Ellipse
            {
                Width = 12,
                Height = 10,
                Fill = bodyBrush,
                Stroke = legBrush,
                StrokeThickness = 1
            };
            Canvas.SetLeft(head, 15);
            Canvas.SetTop(head, 13);

            // Giftklauen (Cheliceren) unten + rote Spinnenaugen
            Path fangs = new Path
            {
                Data = Geometry.Parse("M 18,22 L 16,27 L 19,25 M 24,22 L 26,27 L 23,25"),
                Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5C07B")),
                StrokeThickness = 1.4
            };

            Ellipse eye1 = new Ellipse { Width = 2.2, Height = 2.2, Fill = Brushes.Red };
            Canvas.SetLeft(eye1, 18); Canvas.SetTop(eye1, 19);
            Ellipse eye2 = new Ellipse { Width = 2.2, Height = 2.2, Fill = Brushes.Red };
            Canvas.SetLeft(eye2, 22); Canvas.SetTop(eye2, 19);

            c.Children.Add(legs);
            c.Children.Add(abdomen);
            c.Children.Add(head);
            c.Children.Add(fangs);
            c.Children.Add(eye1);
            c.Children.Add(eye2);
            return c;
        }

        /// <summary>
        /// 5. FUS-RO-DAH THU'UM-SCHUSS: Dreistufige Überschall-Runenwelle mit leuchtender Aura.
        /// </summary>
        private static FrameworkElement CreateDetailedThuumWave()
        {
            Canvas c = new Canvas { Width = 22, Height = 18 };
            Path wave = new Path
            {
                Data = Geometry.Parse("M 1,10 Q 11,0 21,10 M 4,14 Q 11,6 18,14 M 8,17 Q 11,12 14,17"),
                Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#61AFEF")),
                StrokeThickness = 2.4,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };
            c.Children.Add(wave);
            c.Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#61AFEF"),
                BlurRadius = 8,
                ShadowDepth = 0,
                Opacity = 0.9
            };
            return c;
        }

        /// <summary>
        /// 6. DRACHENFEUER-GESCHOSS: Flammen-Tropfen mit heißem goldenen Kern und rotem Feuerschweif.
        /// </summary>
        private static FrameworkElement CreateDragonFireballVisual()
        {
            Canvas c = new Canvas { Width = 10, Height = 16 };
            Path outerFlame = new Path
            {
                Data = Geometry.Parse("M 5,0 L 9,9 A 4.5,4.5 0 1 1 1,9 Z"),
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E06C75"))
            };
            Ellipse innerCore = new Ellipse
            {
                Width = 5,
                Height = 5,
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5C07B"))
            };
            Canvas.SetLeft(innerCore, 2.5);
            Canvas.SetTop(innerCore, 8);
            c.Children.Add(outerFlame);
            c.Children.Add(innerCore);
            return c;
        }

        private void OnWindowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left || e.Key == Key.A) _moveLeft = true;
            if (e.Key == Key.Right || e.Key == Key.D) _moveRight = true;
            if (e.Key == Key.Space) _spacePressed = true;
            if (e.Key == Key.Return && _isGameOver) StartNewGame();
            if (e.Key == Key.Escape) Close();
        }

        private void OnWindowKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left || e.Key == Key.A) _moveLeft = false;
            if (e.Key == Key.Right || e.Key == Key.D) _moveRight = false;
            if (e.Key == Key.Space) _spacePressed = false;
        }
    }
}