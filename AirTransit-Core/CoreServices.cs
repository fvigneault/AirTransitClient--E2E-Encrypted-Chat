﻿using AirTransit_Core.Models;
using AirTransit_Core.Repositories;
using AirTransit_Core.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AirTransit_Core
{
    public class CoreServices
    {
        public IContactRepository ContactRepository { get; private set; }
        public IMessageRepository MessageRepository { get; private set; }
        public IMessageService MessageService { get; private set; }
        public Encoding Encoding { get; } = Encoding.UTF8;
        
        private readonly BlockingCollection<string> _blockingCollection;
        
        private IAuthenticationService _authenticationService;
        private IKeySetRepository _keySetRepository;
        private MessagingContext _messagingContext;
        private IEncryptionService _encryptionService;

        public static string SERVER_ADDRESS = "jo2server.ddns.net:5000";
        
        public CoreServices()
        {
            _blockingCollection = new BlockingCollection<string>();
        }

        public bool Init(string phoneNumber)
        {
            this._messagingContext = new DesignTimeDbContextFactory().CreateDbContext(new string[] { });
            InitializeRepositories(phoneNumber, this._messagingContext);
            this._authenticationService = new AuthenticationService(this._keySetRepository);

            var keySet = _authenticationService.SignUp(phoneNumber);
            if (keySet == null) return false;
            InitializeServices(keySet);
            _messageFetcher = new MessageFetcher(ReceiveNewMessages, TimeSpan.FromMilliseconds(1000), phoneNumber, "TODO la authSignature de hugo");
            return true;

        }

        private void ReceiveNewMessages(IEnumerable<EncryptedMessage> encryptedMessage)
        {
            foreach (EncryptedMessage encryptMessage in encryptedMessage)
            {
                // 1. decrypt message

                // 2. Ajouter le contact s'il n'existe pas deja

                // 3. Ajouter le message dans la BD

                // 4. push le nouveau message créer dans la blocking collection
                _blockingCollection.Add(new Message());
            }

        }

        public BlockingCollection<Message> GetBlockingCollection()
        {
            return _blockingCollection;
        }

        private void InitializeRepositories(string phoneNumber, MessagingContext messagingContext)
        {
            ContactRepository = new EntityFrameworkContactRepository(phoneNumber, messagingContext);
            MessageRepository = new EntityFrameworkMessageRepository(messagingContext);
            this._keySetRepository = new EntityFrameworkKeySetRepository(phoneNumber, this._messagingContext);
            this._encryptionService = new RSAEncryptionService(this._keySetRepository, this.Encoding);
        }

        private void InitializeServices(KeySet keySet)
        {
            MessageService = new MessageService(MessageRepository, this._encryptionService, keySet, Encoding);
        }
    }
}
