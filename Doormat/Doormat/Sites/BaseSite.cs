﻿using DoormatCore.Games;
using DoormatCore.Helpers;
using DoormatCore.Storage;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace DoormatCore.Sites
{


    public abstract class BaseSite
    {

        public List<TriggerAction> ActiveActions { get; set; } = new List<TriggerAction>();
        public LoginParameter[] StaticLoginParams = new LoginParameter[] { new LoginParameter("Username", false, true, false, false), new LoginParameter("Password", true, true, false, true), new LoginParameter("Two Factor Code", false, false, true, true,true) };
        public LoginParameter[] LoginParams { get { return StaticLoginParams; } }
        #region Properties
        /// <summary>
        /// Specifies wether the user can register a new account on the website using the bot.
        /// </summary>
        public bool CanRegister { get; protected set; }

        /// <summary>
        /// Specifies wether the bot is able to withdraw from the specified site.
        /// </summary>
        public bool AutoWithdraw { get; protected set; }

        /// <summary>
        /// Specifies whether the bot can invest coins into the site, if the site supports the feature.
        /// </summary>
        public bool AutoInvest { get; protected set; }

        /// <summary>
        /// Specifies whether the bot can reset the seed for the player.
        /// </summary>
        public bool CanChangeSeed { get; protected set; }

        /// <summary>
        /// Specifies Whether the bot can set the client seed for the current or next seed.
        /// </summary>
        public bool CanSetClientSeed { get; protected set; }

        /// <summary>
        /// Specifies whether the bot can send a tip to another player, if the site supports the feature.
        /// </summary>
        public bool CanTip { get; protected set; }

        /// <summary>
        /// Specify whether tipping on the site uses a username (true, string) or a userID (false, int)
        /// </summary>
        public bool TipUsingName { get; protected set; }

        /// <summary>
        /// Specify whether the bot can fetch the server seed for a specific bet
        /// </summary>
        public bool CanGetSeed { get; protected set; }

        /// <summary>
        /// True if the bot is busy getting the server seed for a specific bet
        /// </summary>
        public bool GettingSeed { get; protected set; }

        /// <summary>
        /// Specifies whether the roll verifier for the site is implemented and working.
        /// </summary>
        public bool CanVerify { get; protected set; }

        /// <summary>
        /// The Reflink URL of the site
        /// </summary>
        public string SiteURL { get; protected set; }

        /// <summary>
        /// The Name of the site
        /// </summary>
        public string SiteName { get; protected set; }

        /// <summary>
        /// The URL where more details for a bet can be seen, using string.format formatting, where {0} is the betID.
        /// </summary>
        public string DiceBetURL { get; protected set; }

        /// <summary>
        /// The index of the list of supported currencies for the current currency
        /// </summary>
        public int Currency { get; set; }

        /// <summary>
        /// The name/abbreviation of the currency currently in use
        /// </summary>
        public string CurrentCurrency { get { return Currencies[Currency]; } }

        /// <summary>
        /// The maximum roll allowed at the site. Usually 99.99. Used to determine whether the roll is a win
        /// </summary>
        public decimal MaxRoll { get; protected set; }

        /// <summary>
        /// The house edge for the site. Used to determine payout and profit for bets and simulations
        /// </summary>
        public decimal Edge { get; protected set; }

        /// <summary>
        /// List of currencies supported by the site
        /// </summary>
        public string[] Currencies { get; protected set; }

        /// <summary>
        /// Indicates whether the bot can connect to and use the chat on the site
        /// </summary>
        public bool CanChat { get; protected set; }

        /// <summary>
        /// Site Statistics about the user 
        /// </summary>
        public SiteStats Stats { get; protected set; }

        SiteDetails siteDetails = null;
        public SiteDetails SiteDetails {
            get
            {
                if (siteDetails==null)
                {
                    siteDetails = new SiteDetails {
                         caninvest=AutoInvest,
                          canresetseed=CanChangeSeed,
                           cantip=CanTip,
                            canwithdraw=AutoWithdraw,
                             edge=Edge,
                              maxroll=MaxRoll,
                               name=SiteName,
                                siteurl=SiteURL,
                                 tipusingname=TipUsingName
                    };
                }
                return siteDetails;
            }
        }

        public string SiteAbbreviation { get; set; }

        public Random R { get; internal set; } = new Random();
        #endregion

        public bool ForceUpdateStats { get; protected set; }

        public Games.Games[] SupportedGames { get; set; } = new Games.Games[] { Games.Games.Dice };

        #region Required Methods

        /// <summary>
        /// Interface with site to handle login.
        /// </summary>
        /// <param name="LoginParams">The login details required for logging in. Typically username, passwordm, 2fa in that order, or API Key</param>
        protected abstract void _Login(LoginParamValue[] LoginParams);

        /// <summary>
        /// Logs the user into the site if correct details were provided
        /// </summary>
        /// <param name="LoginParams">The login details required for logging in. Typically username, passwordm, 2fa in that order, or API Key</param>
        public void LogIn(LoginParamValue[] LoginParams)
        {

            /*bool Success =*/
            _Login(LoginParams);
            UpdateStats();
            //LoginFinished?.Invoke(this, new LoginFinishedEventArgs(Success, this.Stats));
        }

        /// <summary>
        /// Interface with site to disconnect and dispose of applicable objects
        /// </summary>
        protected abstract void _Disconnect();

        /// <summary>
        /// Disconnect from the site, if connected
        /// </summary>
        public void Disconnect()
        {

        }

        /// <summary>
        /// Set the proxy for the connection to the site
        /// </summary>
        /// <param name="ProxyInfo"></param>
        public abstract void SetProxy(Helpers.ProxyDetails ProxyInfo);

        /// <summary>
        /// Update the site statistics for whatever reason.
        /// </summary>
        public void UpdateStats()
        {
            _UpdateStats();

            StatsUpdated?.Invoke(this, new StatsUpdatedEventArgs(this.Stats));
        }

        /// <summary>
        /// Interface with the site to get the latest user stats
        /// </summary>
        protected abstract void _UpdateStats();
        #endregion

        #region Betting methods
        public void PlaceBet(PlaceBet BetDetails)
        {
            if (BetDetails is PlaceDiceBet)
            {
                Thread BetThread = new Thread(new ParameterizedThreadStart(StartDiceThread));
                BetThread.Start(BetDetails);
            }
            if (BetDetails is PlaceCrashBet)
            {
                Thread BetThread = new Thread(new ParameterizedThreadStart(StartCrashThread));
                BetThread.Start(BetDetails);
            }
            if (BetDetails is PlacePlinkoBet)
            {
                Thread BetThread = new Thread(new ParameterizedThreadStart(StartPlinkoThread));
                BetThread.Start(BetDetails);
            }
            if (BetDetails is PlaceRouletteBet)
            {
                Thread BetThread = new Thread(new ParameterizedThreadStart(StartRouletteThread));
                BetThread.Start(BetDetails);
            }
        }


        /// <summary>
        /// Tell the site interface to place the bet. Required function for basic operation of the bot. 
        /// </summary>
        /// <param name="Amount">Amount to bet in full Coin.</param>
        /// <param name="Chance">Chance to win (0-maxroll-edge)</param>
        /// <param name="High">Roll High/Over or Low/Under</param>
        protected virtual void _PlaceDiceBet(PlaceDiceBet BetDetails) { throw new NotImplementedException(); } 


        /// <summary>
        /// Thread starting point for placing a bet. Calls the sites place bet method.
        /// </summary>
        /// <param name="BetDetails"></param>
        private void StartDiceThread(object BetDetails)
        {
            _PlaceDiceBet(BetDetails as PlaceDiceBet);
        }

        /// <summary>
        /// Tell the site interface to place the bet. Required function for basic operation of the bot. 
        /// </summary>
        /// <param name="Amount">Amount to bet in full Coin.</param>
        /// <param name="Chance">Chance to win (0-maxroll-edge)</param>
        /// <param name="High">Roll High/Over or Low/Under</param>
        protected virtual void _PlaceCrasheBet(PlaceCrashBet BetDetails) { throw new NotImplementedException(); } 


        /// <summary>
        /// Thread starting point for placing a bet. Calls the sites place bet method.
        /// </summary>
        /// <param name="BetDetails"></param>
        private void StartCrashThread(object BetDetails)
        {
            _PlaceCrasheBet(BetDetails as PlaceCrashBet);
        }

        /// <summary>
        /// Tell the site interface to place the bet. Required function for basic operation of the bot. 
        /// </summary>
        /// <param name="Amount">Amount to bet in full Coin.</param>
        /// <param name="Chance">Chance to win (0-maxroll-edge)</param>
        /// <param name="High">Roll High/Over or Low/Under</param>
        protected virtual void _PlaceRouletteBet(PlaceRouletteBet BetDetails) { throw new NotImplementedException(); } 


        /// <summary>
        /// Thread starting point for placing a bet. Calls the sites place bet method.
        /// </summary>
        /// <param name="BetDetails"></param>
        private void StartRouletteThread(object BetDetails)
        {
            _PlaceRouletteBet(BetDetails as PlaceRouletteBet);
        }

        /// <summary>
        /// Tell the site interface to place the bet. Required function for basic operation of the bot. 
        /// </summary>
        /// <param name="Amount">Amount to bet in full Coin.</param>
        /// <param name="Chance">Chance to win (0-maxroll-edge)</param>
        /// <param name="High">Roll High/Over or Low/Under</param>
        protected virtual void _PlacePlinkoBet(PlacePlinkoBet BetDetails) { throw new NotImplementedException(); }
        

        /// <summary>
        /// Thread starting point for placing a bet. Calls the sites place bet method.
        /// </summary>
        /// <param name="BetDetails"></param>
        private void StartPlinkoThread(object BetDetails)
        {
            _PlacePlinkoBet(BetDetails as PlacePlinkoBet);
        }
        #endregion

        #region Extention Methods
        public void ResetSeed(string ClientSeed)
        {
            ActiveActions.Add(TriggerAction.ResetSeed);
            if (CanChangeSeed)
            {
                _ResetSeed();
                if (CanSetClientSeed)
                {
                    SetClientSeed(ClientSeed);
                }
            }
            else
                callError("Reset Seed not allowed!", false, ErrorType.NotImplemented);
        }
        protected virtual void _ResetSeed() { }

        public void SetClientSeed(string ClientSeed)
        {
            if (CanSetClientSeed)
            {
                _SetClientSeed(ClientSeed);
            }
            else
                callError("Setting Client Seed not allowed!", false, ErrorType.NotImplemented);
        }
        protected virtual void _SetClientSeed(string ClientSeed) { }

        public void Invest(decimal Amount)
        {
            ActiveActions.Add(TriggerAction.Invest);
            if (AutoInvest)
            {
                _Invest(Amount);
                UpdateStats();
            }
            else
                callError("Investing not allowed!", false, ErrorType.NotImplemented);
        }
        protected virtual void _Invest(decimal Amount) { }

        public void Donate(decimal Amount)
        {
            //ActiveActions.Add(TriggerAction.Donate);
            if (AutoWithdraw || CanTip)
            {
                _Donate(Amount);
                UpdateStats();
            }
            else
                callError("Donations not Implemented!", false, ErrorType.NotImplemented);
        }
        protected virtual void _Donate(decimal Amount) { }

        public void Withdraw(string Address, decimal Amount)
        {
            ActiveActions.Add(TriggerAction.Withdraw);
            if (AutoWithdraw)
            {
                _Withdraw(Address, Amount);
                UpdateStats();
            }
            else
                callError("Withdrawing not allowed!", false, ErrorType.NotImplemented);
        }
        protected virtual void _Withdraw(string Address, decimal Amount) { }

        public void Register(string Username, string Password)
        {
            if (CanRegister)
            {
                bool Success = _Register(Username, Password);
                UpdateStats();
                RegisterFinished?.Invoke(this, new GenericEventArgs { Success = Success });

            }
            else
                callError("Registering not allowed!", false, ErrorType.NotImplemented);

        }
        protected virtual bool _Register(string Username, string Password) { return false; }

        public decimal GetLucky(string Hash, string ServerSeed, string ClientSeed, int Nonce)
        {
            return _GetLucky(Hash, ServerSeed, ClientSeed, Nonce);
        }
        protected virtual decimal _GetLucky(string Hash, string ServerSeed, string ClientSeed, int Nonce)
        {
            HMACSHA512 betgenerator = new HMACSHA512();

            int charstouse = 5;
            List<byte> serverb = new List<byte>();

            for (int i = 0; i < ServerSeed.Length; i++)
            {
                serverb.Add(Convert.ToByte(ServerSeed[i]));
            }

            betgenerator.Key = serverb.ToArray();

            List<byte> buffer = new List<byte>();
            string msg = /*nonce.ToString() + ":" + */ClientSeed + ":" + Nonce.ToString();
            foreach (char c in msg)
            {
                buffer.Add(Convert.ToByte(c));
            }

            byte[] hash = betgenerator.ComputeHash(buffer.ToArray());

            StringBuilder hex = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash)
                hex.AppendFormat("{0:x2}", b);


            for (int i = 0; i < hex.Length; i += charstouse)
            {

                string s = hex.ToString().Substring(i, charstouse);

                decimal lucky = int.Parse(s, System.Globalization.NumberStyles.HexNumber);
                if (lucky < 1000000)
                    return (decimal)(lucky / 10000m);
            }
            return 0;
        }

        public virtual string GetHash(string ServerSeed)
        {
            return Hash.SHA256(ServerSeed);
        }
        public virtual string GenerateNewClientSeed()
        {
            string ClientSeed = R.Next(0, int.MaxValue).ToString();
            return ClientSeed;
        }

        public string GetSeed(long BetID)
        {
            if (CanGetSeed)
            {
                return _GetSeed(BetID);
            }
            else
            {

                callError("Getting server seed not allowed!", false, ErrorType.NotImplemented);
                return "-1";
            }
        }
        protected virtual string _GetSeed(long BetID) { return "-1"; }

        public void SendTip(string Username, decimal Amount)
        {
            ActiveActions.Add(TriggerAction.Tip);
            if (CanTip)
            {
                _SendTip(Username, Amount);
            }
            else
                callError("Tipping not allowed!", false, ErrorType.NotImplemented);
        }
        protected virtual void _SendTip(string Username, decimal Amount) { }

        public void SendChat(string Message)
        {
            if (CanChat)
            {
                _SendChat(Message);
            }
            else
                callError("Chatting not allowed!", false, ErrorType.NotImplemented);
        }
        protected virtual void _SendChat(string Message) { }

        public virtual int _TimeToBet(PlaceBet BetDetails)
        {
            return -1;
        }

        public int TimeToBet(PlaceBet BetDetails)
        {
            return _TimeToBet(BetDetails);
        }

        private bool nonceBased;

        public bool NonceBased
        {
            get { return nonceBased; }
            set { nonceBased = value; }
        }

        #endregion

        #region Events
        public delegate void dStatsUpdated(object sender, StatsUpdatedEventArgs e);
        public delegate void dBetFinished(object sender, BetFinisedEventArgs e);
        public delegate void dLoginFinished(object sender, LoginFinishedEventArgs e);
        public delegate void dRegisterFinished(object sender, GenericEventArgs e);
        public delegate void dError(object sender, ErrorEventArgs e);
        public delegate void dNotify(object sender, GenericEventArgs e);
        public delegate void dAction(object sender, GenericEventArgs e);
        public delegate void dChat(object sender, GenericEventArgs e);
        
        public event dStatsUpdated StatsUpdated;
        public event dBetFinished BetFinished;
        public event dLoginFinished LoginFinished;
        public event dRegisterFinished RegisterFinished;
        public event dError Error;
        public event dNotify Notify;
        public event dAction Action;
        public event dChat ChatReceived;
        public event dAction OnWithdrawalFinished;
        public event dAction OnTipFinished;
        public event dAction OnResetSeedFinished;
        public event dAction OnDonationFinished;
        public event dAction OnInvestFinished;

        protected void callStatsUpdated(SiteStats Stats)
        {
            if (StatsUpdated != null)
            {
                StatsUpdated(this, new StatsUpdatedEventArgs(Stats));
            }
        }
        protected void callBetFinished(Bet NewBet)
        {
            if (BetFinished != null)
            {
                BetFinished(this, new BetFinisedEventArgs(NewBet));
            }
        }
        protected void callLoginFinished(bool Success)
        {
            if (LoginFinished != null)
            {
                LoginFinished(this, new LoginFinishedEventArgs(Success, this.Stats));
            }
        }
        protected void callRegisterFinished(bool Success)
        {

            RegisterFinished?.Invoke(this, new GenericEventArgs { Success = Success });

        }
        protected void callError(string Message, bool Fatal, ErrorType type)
        {
            if (Error != null)
            {
                Error(this, new ErrorEventArgs { Message = Message, Fatal = Fatal, Type= type });
            }
        }
        protected void callNotify(string Message)
        {
            if (Notify != null)
            {
                Notify(this, new GenericEventArgs { Message = Message });
            }
        }
        protected void callAction(string CurrentAction)
        {
            if (Action != null)
            {
                Action(this, new GenericEventArgs { Message = CurrentAction });
            }
        }
        protected void callChatReceived(string Message)
        {
            if (ChatReceived != null)
            {
                ChatReceived(this, new GenericEventArgs { Message = Message });
            }
        }
        protected void callWithdrawalFinished(bool Success, string Message)
        {
            if (ActiveActions.Contains(TriggerAction.Withdraw))
                ActiveActions.Remove(TriggerAction.Withdraw);
            OnWithdrawalFinished?.Invoke(this, new GenericEventArgs { Success = Success, Message = Message });
        }
        protected void callTipFinished(bool Success, string Message)
        {
            if (ActiveActions.Contains(TriggerAction.Withdraw))
                ActiveActions.Remove(TriggerAction.Withdraw);
            OnWithdrawalFinished?.Invoke(this, new GenericEventArgs { Success = Success, Message = Message });
        }
        protected void callResetSeedFinished(bool Success, string Message)
        {
            if (ActiveActions.Contains(TriggerAction.Withdraw))
                ActiveActions.Remove(TriggerAction.Withdraw);
            OnWithdrawalFinished?.Invoke(this, new GenericEventArgs { Success = Success, Message = Message });
        }
        protected void callDonationFinished(bool Success, string Message)
        {
            if (ActiveActions.Contains(TriggerAction.Withdraw))
                ActiveActions.Remove(TriggerAction.Withdraw);
            OnWithdrawalFinished?.Invoke(this, new GenericEventArgs { Success = Success, Message = Message });
        }
        protected void callInvestFinished(bool Success, string Message)
        {
            if (ActiveActions.Contains(TriggerAction.Withdraw))
                ActiveActions.Remove(TriggerAction.Withdraw);
            OnWithdrawalFinished?.Invoke(this, new GenericEventArgs { Success = Success, Message = Message });
        }

        #endregion
        [PersistentTableName("LOGINPARAMETER")]
        public class LoginParameter:PersistentBase
        {
            public LoginParameter(string Name, bool Masked, bool Required, bool ClearafterEnter, bool Clearafterlogin, bool ismfa=false)
            {
                this.Name = Name;
                this.Masked = Masked;
                this.Required = Required;
                this.ClearAfterEnter = ClearafterEnter;
                this.ClearAfterLogin = Clearafterlogin;
                this.IsMFA = IsMFA;
            }

            public LoginParameter()
            {
            }

            public string Name { get; set; }
            public bool Masked { get; set; }
            public bool Required { get; set; }
            public bool ClearAfterEnter { get; set; }
            public bool ClearAfterLogin { get; set; }
            public bool IsMFA { get; set; }
        }
        [PersistentTableName("LOGINPARAMVALUE")]
        public class LoginParamValue: PersistentBase
        {
            [NonPersistent]
            public int ParameterId { get; set; }
            public LoginParameter Param { get; set; }
            public string Value { get; set; }
        }
    }

    public enum ErrorType
    {
        InvalidBet,
        BalanceTooLow,
        ResetSeed,
        Withdrawal,
        Tip,
        NotImplemented,
        Other,
        BetMismatch,
        Unknown
    }

    public class ErrorEventArgs: EventArgs
    {
        public bool Fatal { get; set; }
        public ErrorType Type { get; set; }
        public string Message { get; set; }
        public bool Handled { get; set; }
    }

    public class StatsUpdatedEventArgs : EventArgs
    {
        public SiteStats NewStats { get; set; }
        public StatsUpdatedEventArgs(SiteStats Stats)
        {
            this.NewStats = Stats;
        }
    }
    public class BetFinisedEventArgs : EventArgs
    {
        public Bet NewBet { get; set; }
        public BetFinisedEventArgs(Bet Bet)
        {
            NewBet = Bet;
        }
    }
    public class LoginFinishedEventArgs : EventArgs
    {
        public bool Success { get; set; }
        public SiteStats Stats { get; set; }
        public LoginFinishedEventArgs(bool Success, SiteStats Stats)
        {
            this.Success = Success;
            this.Stats = Stats;
        }
    }
    public class GenericEventArgs : EventArgs
    {
        public string Message { get; set; }
        public bool Success { get; set; }
        public bool Fatal { get; set; }

    }

    public class SiteStats
    {
        public Currency Currency { get; set; }
        public decimal Balance { get; set; }        
        public Games.Games Game { get; set; }
        public decimal Wagered { get; set; }
        public decimal Profit { get; set; }
        public long Bets { get; set; }
        public long Wins { get; set; }
        public long Losses { get; set; }
    }
    
    public class SiteDetails
    {
        public string name { get; set; }
        public decimal edge { get; set; }
        public decimal maxroll { get; set; }
        public bool cantip { get; set; }
        public bool tipusingname { get; set; }
        public bool canwithdraw { get; set; }
        public bool canresetseed { get; set; }
        public bool caninvest { get; set; }
        public string siteurl { get; set; }       
        
    }

}