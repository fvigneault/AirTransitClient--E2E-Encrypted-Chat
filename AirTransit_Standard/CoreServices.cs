﻿using AirTransit_Standard.Models;
using AirTransit_Standard.Repositories;
using AirTransit_Standard.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AirTransit_Standard
{
    public class CoreServices
    {
        public IContactRepository ContactRepository { get; private set; }
        public IMessageRepository MessageRepository { get; private set; }
        public IMessageService MessageService { get; private set; }
        public Encoding Encoding { get; } = Encoding.UTF8;
        
        private readonly BlockingCollection<Message> _blockingCollection;
        
        private IAuthenticationService _authenticationService;
        private IKeySetRepository _keySetRepository;
        private MessagingContext _messagingContext;
        private MessageFetcher _messageFetcher;
        private IEncryptionService _encryptionService;
        
        public CoreServices()
        {
            _blockingCollection = new BlockingCollection<Message>();
        }

        public bool Init(string phoneNumber)
        {
            this._messagingContext = new DesignTimeDbContextFactory().CreateDbContext(new string[] { });
            InitializeRepositories(phoneNumber, this._messagingContext);
            this._authenticationService = new AuthenticationService(this._keySetRepository, phoneNumber);

            if (!_authenticationService.CheckIfKeysExist())
            {
                if (!_authenticationService.SignUp())
                {
                    // TODO This means a communication to the server failed, maybe send an exception instead?
                    return false;
                }
            }

            MessageService = new MessageService(ContactRepository, MessageRepository, this._encryptionService, Encoding, phoneNumber);
            String signature = _encryptionService.GenerateSignature(phoneNumber);
            _messageFetcher = new MessageFetcher(ReceiveNewMessages, TimeSpan.FromMilliseconds(1000), phoneNumber, signature);
            return true;

        }

        private void ReceiveNewMessages(IEnumerable<EncryptedMessage> encryptedMessages)
        {
            foreach (EncryptedMessage encryptedMessage in encryptedMessages)
            {
                Message message = MessageService.ReceiveNewMessages(encryptedMessage);
                if (message != null)
                {
                    _blockingCollection.Add(message);
                }
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
    }
}