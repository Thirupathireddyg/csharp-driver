﻿// 
//       Copyright DataStax, Inc.
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//       http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// 

using System;
using System.Collections.Generic;

namespace Cassandra.MetadataHelpers
{
    internal class SimpleStrategy : IReplicationStrategy, IEquatable<SimpleStrategy>
    {
        private readonly int _replicationFactor;

        public SimpleStrategy(int replicationFactor)
        {
            _replicationFactor = replicationFactor;
        }

        public Dictionary<IToken, ISet<Host>> ComputeTokenToReplicaMap(
            IDictionary<string, int> replicationFactors, 
            IList<IToken> ring, 
            IDictionary<IToken, Host> primaryReplicas,
            ICollection<Host> hostsWithTokens,
            IDictionary<string, TokenMap.DatacenterInfo> datacenters)
        {
            return ComputeTokenToReplicaSimple(
                replicationFactors["replication_factor"], hostsWithTokens, ring, primaryReplicas);
        }

        public bool Equals(IReplicationStrategy other)
        {
            return Equals(other as SimpleStrategy);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SimpleStrategy);
        }

        public bool Equals(SimpleStrategy other)
        {
            return other != null && _replicationFactor == other._replicationFactor;
        }

        public override int GetHashCode()
        {
            return _replicationFactor;
        }
        
        /// <summary>
        /// Converts token-primary to token-replicas
        /// </summary>
        private Dictionary<IToken, ISet<Host>> ComputeTokenToReplicaSimple(
            int replicationFactor, 
            ICollection<Host> hostsWithTokens, 
            IList<IToken> ring, 
            IDictionary<IToken, Host> primaryReplicas)
        {
            var rf = Math.Min(replicationFactor, hostsWithTokens.Count);
            var tokenToReplicas = new Dictionary<IToken, ISet<Host>>(ring.Count);
            for (var i = 0; i < ring.Count; i++)
            {
                var token = ring[i];
                var replicas = new HashSet<Host>();
                for (var j = 0; j < rf; j++)
                {
                    //circle back if necessary
                    var nextReplicaIndex = (i + j) % ring.Count;
                    var nextReplica = primaryReplicas[ring[nextReplicaIndex]];
                    replicas.Add(nextReplica);
                }
               
                tokenToReplicas.Add(token, replicas);
            }
            return tokenToReplicas;
        }
    }
}