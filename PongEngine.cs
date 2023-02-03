using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Drawing;
using Priority_Queue;

namespace Prong
{
    class PongEngine
    {
        public StaticState Config { get; }

        private DynamicState state;

        public AStarAgent aStarAgent;

        public PongEngine(StaticState config)
        {
            this.Config = config;
        }

        public float clamp(float value, float min, float max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        public float plr1PaddleBallBounceVelocityY()
        {
            float up = state.plr1PaddleY - Config.paddleHeight() / 2.0f;
            float down = state.plr1PaddleY + Config.paddleHeight() / 2.0f;
            float ball = clamp(state.ballY, up, down);
            float ratio = (ball - up) / (down - up);
            ratio = 2 * (ratio - 0.5f);
            ratio = clamp(ratio, -0.9f, 0.9f);
            return ratio;
        }

        public float plr2PaddleBallBounceVelocityY()
        {
            float up = state.plr2PaddleY - Config.paddleHeight() / 2.0f;
            float down = state.plr2PaddleY + Config.paddleHeight() / 2.0f;
            float ball = clamp(state.ballY, up, down);
            float ratio = (ball - up) / (down - up);
            ratio = 2 * (ratio - 0.5f);
            ratio = clamp(ratio, -0.9f, 0.9f);
            return ratio;
        }

        void bounceBall(float ratio, float xDir)
        {
            state.ballVelocityY = Config.ballSpeed * ratio;
            state.ballVelocityX = (float)Math.Sqrt(Config.ballSpeed * Config.ballSpeed - state.ballVelocityY * state.ballVelocityY);
            state.ballVelocityX *= xDir;
            state.ballYDirection = 1;
        }

        public int plr1PaddleBounceX()
        {
            return -Config.ClientSize_Width / 2 + Config.paddleWidth() / 2;
        }

        public int plr2PaddleBounceX()
        {
            return Config.ClientSize_Width / 2 - Config.paddleWidth() / 2;
        }

        public bool ballFlyingRight()
        {
            return state.ballVelocityX > 0;
        }

        public bool ballHitsRightPaddle()
        {
            return state.ballX + Config.gridCellSize / 2 > plr2PaddleBounceX() - Config.paddleWidth() / 2
                && state.ballY - Config.gridCellSize / 2 < state.plr2PaddleY + Config.paddleHeight() / 2
                && state.ballY + Config.gridCellSize / 2 > state.plr2PaddleY - Config.paddleHeight() / 2;
        }

        public bool ballPastPlayer2Edge()
        {
            return state.ballX > Config.ClientSize_Width / 2;
        }

        public bool ballHitsLeftPaddle()
        {
            return state.ballX - Config.gridCellSize / 2 < plr1PaddleBounceX() + Config.paddleWidth() / 2
                && state.ballY - Config.gridCellSize / 2 < state.plr1PaddleY + Config.paddleHeight() / 2
                && state.ballY + Config.gridCellSize / 2 > state.plr1PaddleY - Config.paddleHeight() / 2;
        }

        public bool ballPastPlayer1Edge()
        {
            return state.ballX < -Config.ClientSize_Width / 2;
        }

        public bool ballHitsTop()
        {
            return state.ballVelocityY * state.ballYDirection >= 0 && state.ballY + Config.gridCellSize / 2 > Config.ClientSize_Height / 2;
        }

        public bool ballHitsBottom()
        {
            return state.ballVelocityY * state.ballYDirection <= 0 && state.ballY - Config.gridCellSize / 2 < -Config.ClientSize_Height / 2;
        }

        void moveBall(float timeDelta)
        {
            state.ballX += state.ballVelocityX * timeDelta;
            state.ballY += state.ballVelocityY * state.ballYDirection * timeDelta;
        }

        void resetBall()
        {
            state.ballX = 0;
            state.ballY = 0;
            int sign = -Math.Sign(state.ballVelocityX);
            state.ballVelocityX = sign * 400;
            state.ballVelocityY = 300;
        }

        public void SetState(DynamicState state)
        {
            this.state = state;
        }

        public TickResult Tick(DynamicState state, PlayerAction plr1, PlayerAction plr2, float timeDelta)
        {
            SetState(state);
            TickResult result = tickMechanics(timeDelta);
            tickPlayersActions(plr1, plr2, timeDelta);
            return result;
        }

        private TickResult tickMechanics(float timeDelta)
        {
            moveBall(timeDelta);

            if (ballFlyingRight())
            {
                if (ballHitsRightPaddle())
                {
                    bounceBall(plr2PaddleBallBounceVelocityY(), -1);
                }
                if (ballPastPlayer2Edge())
                {
                    state.plr1Score += 1;
                    resetBall();
                    return TickResult.PLAYER_1_SCORED;
                }
            }
            else
            {
                if (ballHitsLeftPaddle())
                {
                    bounceBall(plr1PaddleBallBounceVelocityY(), 1);
                }
                if (ballPastPlayer1Edge())
                {
                    state.plr2Score += 1;
                    resetBall();
                    return TickResult.PLAYER_2_SCORED;
                }
            }

            if (ballHitsTop())
            {
                state.ballYDirection *= -1;
            }

            if (ballHitsBottom())
            {
                state.ballYDirection *= -1;
            }

            return TickResult.TICK;
        }


        private void tickPlayersActions(PlayerAction plr1, PlayerAction plr2, float timeDelta)
        {
            //PLAYER1 USER INPUT SYSTEM

            //if (plr1 == PlayerAction.UP)
            //{
            //    state.plr1PaddleY = state.plr1PaddleY + Config.paddle1Speed * timeDelta;
            //}
            //if (plr1 == PlayerAction.DOWN)
            //{
            //    state.plr1PaddleY = state.plr1PaddleY - Config.paddle1Speed * timeDelta;
            //}

            /*PLAYER1 REACTIVE BALL TRACKING*/

            float ballX = state.ballX;
            float ballY = state.ballY;

            float paddleY = state.plr1PaddleY;

            float futureBallX = ballX + state.ballVelocityX * timeDelta;
            float futureBallY = ballY + state.ballVelocityY * timeDelta;

            if (futureBallY > Config.ClientSize_Height)
            {
                futureBallY = Math.Max(0, Math.Min(Config.ClientSize_Height, futureBallY));
                state.ballVelocityY = -state.ballVelocityY;
            }

            if (futureBallX < plr1PaddleBounceX())
            {
                if (Math.Abs(futureBallY - state.plr1PaddleY) < Config.paddleHeight() / 2)
                {
                    futureBallX = plr1PaddleBounceX();
                    state.ballVelocityX = -state.ballVelocityX;
                }
            }

            float distance = futureBallY - paddleY;

            if (distance > 0)
            {
                state.plr1PaddleY += Config.paddle1Speed * timeDelta *0.76f;
            }
            else if (distance < 0)
            {
                state.plr1PaddleY -= Config.paddle1Speed * timeDelta *0.76f;
            }

            /*PLAYER2 RULE-BASED FORWARD MODEL AI*/

            float futureBallX2 = state.ballX + state.ballVelocityX * timeDelta;
            float futureBallY2 = state.ballY + state.ballVelocityY * timeDelta;

            if (futureBallY2 > state.plr2PaddleY + Config.paddleHeight() / 2)
            {
                state.plr2PaddleY += Config.paddle1Speed * timeDelta;
            }
            else if (futureBallY2 < state.plr2PaddleY - Config.paddleHeight() / 2)
            {
                state.plr2PaddleY -= Config.paddle1Speed * timeDelta;
            }

            /*PLAYER2 A* BASEDA AGENT IMPLEMENTATION*/

            aStarAgent = new AStarAgent();
            Vector2 ballPos = new Vector2(state.ballX, state.ballY);
            Vector2 paddlePos = new Vector2(0, state.plr2PaddleY);
            Vector2 screenSize = new Vector2(Config.ClientSize_Height, Config.ClientSize_Width);
            Vector2 paddleSize = new Vector2(Config.paddleHeight(), Config.paddleWidth());
            aStarAgent.SetInitialPositions(ballPos, paddlePos, screenSize, paddleSize);

            Vector2 opponentPaddlePos = new Vector2(0, state.plr1PaddleY);
            Vector2 bestMove = aStarAgent.FindBestMove(opponentPaddlePos);

            paddlePos = bestMove;

            //PLAYER2 USER INPUT SYSTEM

            //if (plr2 == PlayerAction.UP)
            //{
            //    state.plr2PaddleY = state.plr2PaddleY + Config.paddle2Speed * timeDelta;
            //}
            //if (plr2 == PlayerAction.DOWN)
            //{
            //    state.plr2PaddleY = state.plr2PaddleY - Config.paddle2Speed * timeDelta;
            //}

        }
    }


    class AStarAgent
    {
        private Vector2 ballPosition;
        private Vector2 paddlePosition;

        private Vector2 screenSize;
        private Vector2 paddleSize;

        public void SetInitialPositions(Vector2 ballPos, Vector2 paddlePos, Vector2 screen, Vector2 paddle)
        {
            ballPosition = ballPos;
            paddlePosition = paddlePos;
            screenSize = screen;
            paddleSize = paddle;
        }

        public Vector2 FindBestMove(Vector2 opponentPaddlePos)
        {
            Queue<Node> queue = new Queue<Node>();

            queue.Enqueue(new Node(paddlePosition, 0, Heuristic(paddlePosition, ballPosition)));

            HashSet<Vector2> visited = new HashSet<Vector2>();

            while (queue.Count > 0)
            {
                Node current = queue.Dequeue();

                if (InContact(current.Position, ballPosition, paddleSize))
                    return current.Position;

                visited.Add(current.Position);

                List<Vector2> moves = GenerateMoves(current.Position, opponentPaddlePos);

                for (int i = 0; i < moves.Count; i++)
                {
                    Vector2 move = moves[i];

                    if (IsValidMove(move) && !visited.Contains(move))
                    {
                        queue.Enqueue(new Node(move, current.Cost + Cost(current.Position, move), Heuristic(move, ballPosition)));
                    }
                }
            }

            return paddlePosition;
        }

        private bool IsValidMove(Vector2 move)
        {
            if (move.X < 0 || move.X >= screenSize.X || move.Y < 0 || move.Y >= screenSize.Y)
                return false;

            return true;
        }

        private bool InContact(Vector2 paddlePos, Vector2 ballPos, Vector2 paddleSize)
        {
            Vector2 topLeft = paddlePos;
            Vector2 bottomRight = paddlePos + paddleSize;

            if (ballPos.X >= topLeft.X && ballPos.X <= bottomRight.X
                && ballPos.Y >= topLeft.Y && ballPos.Y <= bottomRight.Y)
                return true;
            else
                return false;
        }

        private List<Vector2> GenerateMoves(Vector2 currentPos, Vector2 opponentPaddlePos)
        {
            List<Vector2> moves = new List<Vector2>();

            // Move up
            Vector2 move = new Vector2(currentPos.X, currentPos.Y - 1);
            if (IsValidMove(move, opponentPaddlePos))
                moves.Add(move);

            // Move down
            move = new Vector2(currentPos.X, currentPos.Y + 1);
            if (IsValidMove(move, opponentPaddlePos))
                moves.Add(move);

            //// Move left (WE WONT BE DOING THAT IN A PONG GAME)
            //move = new Vector2(currentPos.X - 1, currentPos.Y);
            //if (IsValidMove(move, opponentPaddlePos))
            //    moves.Add(move);
            //// Move right (WE WONT BE DOING THAT IN A PONG GAME)
            //move = new Vector2(currentPos.X + 1, currentPos.Y);
            //if (IsValidMove(move, opponentPaddlePos))
            //    moves.Add(move);

            return moves;
        }

        private bool IsValidMove(Vector2 move, Vector2 opponentPaddlePos)
        {
            if (move.X < 0 || move.X >= screenSize.X || move.Y < 0 || move.Y >= screenSize.Y)
                return false;
            if (move.X == opponentPaddlePos.X && move.Y == opponentPaddlePos.Y)
                return false;
            return true;
        }

        private float Cost(Vector2 currentPos, Vector2 nextPos)
        {
            return Vector2.Distance(currentPos, nextPos);
        }

        private float Heuristic(Vector2 currentPos, Vector2 goalPos)
        {
            return Math.Abs(currentPos.X - goalPos.X) + Math.Abs(currentPos.Y - goalPos.Y);
        }

        private class Node : IComparable<Node>
        {
            public Vector2 Position;
            public float Cost;
            public float F;

            public Node(Vector2 pos, float cost, float f)
            {
                Position = pos;
                Cost = cost;
                F = f;
            }

            public int CompareTo(Node other)
            {
                return F.CompareTo(other.F);
            }
        }
    }

}

