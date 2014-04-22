﻿//
//      Copyright (C) 2012 DataStax Inc.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cassandra
{
    public class RowSet : IEnumerable<Row>
    {
        /// <summary>
        /// Event that is fired to get the next page.
        /// </summary>
        public event Func<byte[], RowSet> FetchNextPage;

        /// <summary>
        /// The task that handles the fetching of the next page.
        /// When set it states that is currently fetching.
        /// </summary>
        protected Task FetchNextPageTask = null;

        /// <summary>
        /// Gets or set the internal row list. It contains the rows of the latest query page.
        /// </summary>
        protected virtual List<Row> RowList { get; set; }

        /// <summary>
        /// Gets the execution info of the query
        /// </summary>
        public virtual ExecutionInfo Info { get; set; }

        /// <summary>
        /// Gets or sets the columns in the rowset
        /// </summary>
        public virtual CqlColumn[] Columns { get; set; }

        /// <summary>
        /// Gets or sets the paging state of the query for the rowset.
        /// When set it states that there are more pages.
        /// </summary>
        public virtual byte[] PagingState { get; set; }

        /// <summary>
        /// Determines if all the rows from the previous query have been retrieved
        /// </summary>
        public virtual bool IsExhausted
        {
            get
            {
                if (RowList.Count == 0 || PagingState == null)
                {
                    return true;
                }
                return false;
            }
        }

        public RowSet()
        {
            RowList = new List<Row>();
            Info = new ExecutionInfo();
            Columns = new CqlColumn[] { };
        }

        /// <summary>
        /// Adds a row to the inner row list
        /// </summary>
        internal virtual void AddRow(Row row)
        {
            RowList.Add(row);
        }

        /// <summary>
        /// For backward compatibility: It is possible to iterate using the RowSet as it is enumerable.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Row> GetRows()
        {
            //legacy: Keep the GetRows method for Compatibity.
            return this;
        }

        public IEnumerator<Row> GetEnumerator()
        {
            var enumerator = new RowEnumerator(RowList);
            if (PagingState != null)
            {
                enumerator.MovedToEnd += PageNext;
            }
            return enumerator;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        protected virtual void PageNext()
        {
            if (PagingState == null)
            {
                return;
            }
            if (FetchNextPage == null)
            {
                //Clear the paging state
                PagingState = null;
                return;
            }

            if (FetchNextPageTask == null)
            {
                FetchNextPageTask = Task.Factory.StartNew(() =>
                {
                    var rs = FetchNextPage(this.PagingState);
                    this.PagingState = rs.PagingState;
                    this.FetchNextPageTask = null;
                    this.RowList.AddRange(rs.RowList);
                });
            }
            try
            {
                FetchNextPageTask.Wait();
            }
            catch (AggregateException ex)
            {
                throw ex.InnerException;
            }
        }
    }
}