﻿using System;
using Aquarius.TimeSeries.Client;
using FluentAssertions;
using NUnit.Framework;
using Ploeh.AutoFixture;

namespace Aquarius.UnitTests.TimeSeries.Client
{
    [TestFixture]
    public class ConnectionTests
    {
        private IFixture _fixture;
        private Connection _connection;
        private int _sessionCreateCount;
        private int _sessionDeleteCount;

        [SetUp]
        public void BeforeEachTest()
        {
            _fixture = new Fixture();
            _sessionCreateCount = 0;
            _sessionDeleteCount = 0;

            _connection = new Connection(_fixture.Create<string>(), _fixture.Create<string>(), SessionTokenCreator, DeleteSession);
        }

        private string SessionTokenCreator(string username, string password)
        {
            ++_sessionCreateCount;

            return _fixture.Create(string.Join("/", username, password));
        }

        private void DeleteSession()
        {
            ++_sessionDeleteCount;
        }

        [Test]
        public void NewlyConstructedConnection_TracksOneConnection()
        {
            AssertExpectedConnectionCount(1);
            AssertExpectedSessionCreateCount(1);
            AssertExpectedSessionDeleteCount(0);
        }

        private void AssertExpectedConnectionCount(int expectedCount)
        {
            _connection.ConnectionCount.ShouldBeEquivalentTo(expectedCount, nameof(_connection.ConnectionCount));
        }

        private void AssertExpectedSessionCreateCount(int expectedCount)
        {
            _sessionCreateCount.ShouldBeEquivalentTo(expectedCount, nameof(_sessionCreateCount));
        }

        private void AssertExpectedSessionDeleteCount(int expectedCount)
        {
            _sessionDeleteCount.ShouldBeEquivalentTo(expectedCount, nameof(_sessionDeleteCount));
        }

        [Test]
        public void IncrementConnectionCount_IncrementsConnectionCount()
        {
            _connection.IncrementConnectionCount();

            AssertExpectedConnectionCount(2);
            AssertExpectedSessionCreateCount(1);
            AssertExpectedSessionDeleteCount(0);
        }

        [Test]
        public void Close_WithNonPositiveConnectionCount_DoesNotDeleteSession()
        {
            _connection.ConnectionCount = 0;

            _connection.Close();

            AssertExpectedSessionCreateCount(1);
            AssertExpectedSessionDeleteCount(0);
        }

        [Test]
        public void Close_WithSingleConnectCount_DeletesSession()
        {
            _connection.Close();

            AssertExpectedSessionCreateCount(1);
            AssertExpectedSessionDeleteCount(1);
        }

        [Test]
        public void Close_WithMoreThanOneConnection_DoesNotDeleteSession()
        {
            _connection.ConnectionCount = 2;

            _connection.Close();

            AssertExpectedSessionCreateCount(1);
            AssertExpectedSessionDeleteCount(0);
        }

        [Test]
        public void ReconnectIfIdle_WithoutExpiring_DoesNotReconnect()
        {
            var sessionToken = _connection.SessionToken;

            _connection.ReconnectIfIdle(TimeSpan.MaxValue);

            AssertExpectedSessionCreateCount(1);
            AssertExpectedSessionDeleteCount(0);
            sessionToken.ShouldBeEquivalentTo(_connection.SessionToken, "the same session token should be retained");
        }

        [Test]
        public void ReconnectIfIdle_AfterExpiring_ReconnectsWithNewSession()
        {
            var sessionToken = _connection.SessionToken;

            _connection.ReconnectIfIdle(TimeSpan.Zero);

            AssertExpectedSessionCreateCount(2);
            AssertExpectedSessionDeleteCount(1);
            sessionToken.Should().NotBe(_connection.SessionToken, "a new session token should be created");
        }
    }
}
