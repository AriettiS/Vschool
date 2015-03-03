﻿
namespace ILS.PhotonServer.MasterServer
{
    using System;
    using System.Collections.Generic;

    using ExitGames.Concurrency.Fibers;
    using ExitGames.Logging;

    using ILS.PhotonServer.MasterServer.Lobby;
    using ILS.PhotonServer.Operations;
    using Photon.SocketServer;

    public class PlayerCache : IDisposable
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private readonly PoolFiber fiber = new PoolFiber();

        private readonly Dictionary<string, PlayerState> playerDict = new Dictionary<string,PlayerState>();

        public PlayerCache()
        {
            this.fiber.Start();
        }

        public void OnConnectedToMaster(string playerId)
        {
            this.fiber.Enqueue(() => this.HandleOnConnectedToMaster(playerId));
        }

        public void OnDisconnectFromMaster(string playerId)
        {
            this.fiber.Enqueue(() => this.HandleOnDisconnectFromMaster(playerId));
        }

        public void OnDisconnectFromGameServer(string playerId)
        {
            this.fiber.Enqueue(() => this.HandleOnDisconnectFromGameServer(playerId));
        }

        public void OnJoinedGamed(string playerId, GameState gameState)
        {
            this.fiber.Enqueue(() => this.HandleOnJoinedGamed(playerId, gameState));
        }

        public void FiendFriends(PeerBase peer, FindFriendsRequest request, SendParameters sendParameters)
        {
            this.fiber.Enqueue(() => this.HandleFiendFriends(peer, request, sendParameters));
        }

        private void HandleOnConnectedToMaster(string playerId)
        {
            try
            {
                // only peers with userid set can be handled
                if (string.IsNullOrEmpty(playerId))
                {
                    return;
                }

                PlayerState playerState;
                if (this.playerDict.TryGetValue(playerId, out playerState) == false)
                {
                    playerState = new PlayerState(playerId);
                    this.playerDict.Add(playerId, playerState);
                }

                playerState.IsConnectedToMaster = true;

                if (log.IsDebugEnabled)
                {
                    string gameId = playerState.Game == null ? string.Empty : playerState.Game.Id;
                    log.DebugFormat("Player state changed: pid={0}, master={1}, gid={2}", playerId, playerState.IsConnectedToMaster, gameId);
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        private void HandleOnDisconnectFromMaster(string playerId)
        {
            try
            {
                // only peers with userid set can be handled
                if (string.IsNullOrEmpty(playerId))
                {
                    return;
                }

                PlayerState playerState;
                if (this.playerDict.TryGetValue(playerId, out playerState) == false)
                {
                    return;
                }

                playerState.IsConnectedToMaster = false;
                if (playerState.Game != null)
                {
                    return;
                }

                this.playerDict.Remove(playerId);
                if (log.IsDebugEnabled)
                {
                    string gameId = playerState.Game == null ? string.Empty : playerState.Game.Id;
                    log.DebugFormat("Player removed: pid={0}, master={1}, gid={2}", playerId, playerState.IsConnectedToMaster, gameId);
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        private void HandleOnJoinedGamed(string playerId, GameState gameState)
        {
            try
            {
                // only peers with userid set can be handled
                if (string.IsNullOrEmpty(playerId))
                {
                    return;
                }

                PlayerState playerState;
                if (this.playerDict.TryGetValue(playerId, out playerState) == false)
                {
                    playerState = new PlayerState(playerId);
                    this.playerDict.Add(playerId, playerState);
                }

                playerState.Game = gameState;

                if (log.IsDebugEnabled)
                {
                    string gameId = gameState == null ? string.Empty : gameState.Id;
                    log.DebugFormat("Player state changed: pid={0}, master={1}, gid={2}", playerId, playerState.IsConnectedToMaster, gameId);
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        private void HandleOnDisconnectFromGameServer(string playerId)
        {
            try
            {
                // only peers with userid set can be handled
                if (string.IsNullOrEmpty(playerId))
                {
                    return;
                }

                PlayerState playerState;
                if (this.playerDict.TryGetValue(playerId, out playerState) == false)
                {
                    return;
                }

                playerState.Game = null;
                if (playerState.IsConnectedToMaster)
                {
                    return;
                }

                this.playerDict.Remove(playerId);
                if (log.IsDebugEnabled)
                {
                    string gameId = playerState.Game == null ? string.Empty : playerState.Game.Id;
                    log.DebugFormat("Player removed: pid={0}, master={1}, gid={2}", playerId, playerState.IsConnectedToMaster, gameId);
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        private void HandleFiendFriends(PeerBase peer, FindFriendsRequest request, SendParameters sendParameters)
        {
            try
            {
                var onlineList = new bool[request.UserList.Length];
                var gameIds = new string[request.UserList.Length];

                for (int i = 0; i < request.UserList.Length; i++)
                {
                    PlayerState playerState;
                    if (this.playerDict.TryGetValue(request.UserList[i], out playerState))
                    {
                        onlineList[i] = true;
                        if (playerState.Game != null)
                        {
                            gameIds[i] = playerState.Game.Id;
                        }
                        else
                        {
                            gameIds[i] = string.Empty;
                        }
                    }
                    else
                    {
                        gameIds[i] = string.Empty;
                    }
                }

                var response = new FindFriendsResponse { IsOnline = onlineList, UserStates = gameIds };
                var opResponse = new OperationResponse((byte)OperationCode.FiendFriends, response);
                peer.SendOperationResponse(opResponse, sendParameters);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        public void Dispose()
        {
            var poolFiber = this.fiber;
            if (poolFiber != null)
            {
                poolFiber.Dispose();
            }
        }
    }

    public class PlayerState
    {
        public readonly string PlayerId ;
 
        public PlayerState(string playerId)
        {
            this.PlayerId = playerId;
        }

        public bool IsConnectedToMaster { get; set; }

        public GameState Game { get; set; }
    }
}
