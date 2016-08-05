using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Robocode;
using Robocode.Util;

namespace BR
{

    public class KillBotFiveThousand : AdvancedRobot
    {
        private enum BotState
        {
            Scanning,
            FoundBot
        }

        private BotState _state = BotState.Scanning;

        private bool _fire = false;
        private int _bulletPower = 1;
        private double _moveDistance = 50;

        private double _targetGunAngle = 0;

        private string _currentTarget = string.Empty;

        private bool _targetScanRight = true;
        private int _targetScanDistanceMax = 45;
        private int _targetScanDistanceTraveled;
        private int _targetScanTimeout = 1000;

        private int _radarSpeed = 5;
        private int _targetScanGunSpeed = 5;

        private Stopwatch _targetScanStopwatch = new Stopwatch();

        #region Public Methods

        public override void Run()
        {
            SetColors(Color.DarkCyan, Color.Orange, Color.PaleTurquoise);

            var minSide = Math.Min(BattleFieldHeight, BattleFieldWidth);
            _moveDistance = minSide/5;

            while (true)
            {
                // Kill all humans (and bots)

                if (!_fire)
                {
                    move();
                }
                else
                {
                    slowMove();
                }

                switch (_state)
                {
                    case BotState.Scanning:
                        runScanningLogic();
                        break;
                    case BotState.FoundBot:
                        runBotFollowLogic();
                        break;
                    default:
                        break;
                }

                Execute();
            }
        }

        public override void OnScannedRobot(ScannedRobotEvent bot)
        {
            switchState(BotState.FoundBot);
            _targetScanStopwatch.Reset();
            _targetScanStopwatch.Start();

            if (bot.Distance < 400)
            {
                setBulletPower(bot);
                _fire = true;
                _currentTarget = bot.Name;
            }

            Console.WriteLine("Target Bearing: " + bot.Bearing);
            Console.WriteLine("Target Distance: " + bot.Distance);
            Console.WriteLine("Target Heading: " + bot.Heading);
            Console.WriteLine("Target Velocity: " + bot.Velocity);

            Console.WriteLine("Coords: ({0}, {1})", X, Y);
            Console.WriteLine("Heading: " + Heading);
            Console.WriteLine("Radar Heading: " + RadarHeading);
            Console.WriteLine("Gun Heading: " + GunHeading);
            
            //targetGunAngle = Heading - GunHeading + bot.Bearing;
            _targetGunAngle = Utils.NormalRelativeAngleDegrees(Heading - GunHeading + bot.Bearing);
            _targetScanRight = _targetGunAngle > 0;

            var bulletSpeed = 20 - _bulletPower * 3;
            var time = bot.Distance / bulletSpeed;

            // Put prediction code here

            Console.WriteLine("TARGET GUN ANGLE: " + _targetGunAngle);

            _targetScanDistanceTraveled = 0;
        }

        /// <summary>
        ///     If a robot dies, resets back to scanning
        /// </summary>
        /// <param name="bot"></param>
        public override void OnRobotDeath(RobotDeathEvent bot)
        {
            if (bot.Name == _currentTarget)
            {
                switchState(BotState.Scanning);
                _currentTarget = string.Empty;
            }
        }

        /// <summary>
        ///     If we collide...
        /// </summary>
        /// <param name="bot"></param>
        public override void OnHitRobot(HitRobotEvent bot)
        {
            switchState(BotState.FoundBot);
            _targetScanStopwatch.Reset();
            _targetScanStopwatch.Start();

            _bulletPower = 3;
            _fire = true;

            _targetGunAngle = Utils.NormalRelativeAngleDegrees(Heading - GunHeading + bot.Bearing);
            _targetScanDistanceTraveled = 0;
        }

        #endregion

        #region Private Methods

        private void runScanningLogic()
        {
            SetTurnRadarRight(_radarSpeed);
        }

        private void runBotFollowLogic()
        {
            scanForRobot();

            if (_targetGunAngle != 0)
            {
                Console.WriteLine("TURNING GUN {0} DEGREES", _targetGunAngle);

                SetTurnGunRight(_targetGunAngle);
                SetTurnRadarRight(-_targetGunAngle);
                _targetGunAngle = 0;
            }

            // Only shoot if aiming at target
            if (_fire && Math.Abs(GunTurnRemaining) < 1)
            {
                SetFire(_bulletPower);
                _fire = false;
            }

            // Switch back to scanning if target is lost after timeout
            if (_targetScanStopwatch.ElapsedMilliseconds > _targetScanTimeout)
            {
                switchState(BotState.Scanning);
            }
        }

        private void move()
        {
            SetAhead(50);
            SetTurnRight(5);
            SetTurnRadarRight(-5);
            _targetGunAngle -= 5;
        }

        private void slowMove()
        {
            SetAhead(4);
        }

        private void setBulletPower(ScannedRobotEvent bot)
        {
            if (bot.Distance < 100)
            {
                _bulletPower = 3;
            }
            else if (bot.Distance < 200)
            {
                _bulletPower = 2;
            }
            else
            {
                _bulletPower = 1;
            }
        }

        private void scanForRobot()
        {
            SetTurnRadarRight(_targetScanRight ? _targetScanGunSpeed : -_targetScanGunSpeed);
            _targetScanDistanceTraveled += _targetScanGunSpeed;

            if (_targetScanDistanceTraveled > _targetScanDistanceMax)
            {
                _targetScanRight = !_targetScanRight;
                _targetScanDistanceTraveled = 0;
            }
        }

        private void switchState(BotState newState)
        {
            _state = newState;

            switch (newState)
            {
                case BotState.Scanning:
                    Console.WriteLine("Scanning...");
                    break;
                case BotState.FoundBot:
                    Console.WriteLine("Found bot...");
                    break;
                default:
                    break;
            }
        }

        #endregion
    }
}
