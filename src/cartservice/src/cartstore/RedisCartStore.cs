// Copyright 2018 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Caching.Distributed;
using Google.Protobuf;
using System.Collections.Generic;
using Newtonsoft.Json;
using log4net;
using log4net.Config;

namespace cartservice.cartstore
{
    public class RedisCartStore : ICartStore
    {
        private readonly IDistributedCache _cache;
        private Random rnd;
        private static readonly ILog log = LogManager.GetLogger(typeof(RedisCartStore));

        public RedisCartStore(IDistributedCache cache)
        {
            _cache = cache;
            rnd = new Random();
        }

        public async Task AddItemAsync(string userId, string productId, int quantity)
        {
            Console.WriteLine($"[severity:info] - AddItemAsync called with userId={userId}, productId={productId}, quantity={quantity}");
            var aa = rnd.NextDouble();
            if (true)
            {
                var logLevel = "Lorem ipsum dolor sit amet, consectetuer adipiscing elit. Aenean commodo ligula eget dolor. Aenean massa. Cum sociis natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus. Donec quam felis, ultricies nec, pellentesque eu, pretium quis, sem. Nulla consequat massa quis enim. Donec pede justo, fringilla vel, aliquet nec, vulputate eget, arcu. In enim justo, rhoncus ut, imperdiet a, venenatis vitae, justo. Nullam dictum felis eu pede mollis pretium. Integer tincidunt. Cras dapibus. Vivamus elementum semper nisi. Aenean vulputate eleifend tellus. Aenean leo ligula, porttitor eu, consequat vitae, eleifend ac, enim. Aliquam lorem ante, dapibus in, viverra quis, feugiat a, tellus. Phasellus viverra nulla ut metus varius laoreet. Quisque rutrum. Aenean imperdiet. Etiam ultricies nisi vel augue. Curabitur ullamcorper ultricies nisi. Nam eget dui. Etiam rhoncus. Maecenas tempus, tellus eget condimentum rhoncus, sem quam semper libero, sit amet adipiscing sem neque sed ipsum. Nam quam nunc, blandit vel";
                // var logLevel = "error";

                var unstructuredLog = $"[severity:{logLevel}] - Something went wrong";
                Console.WriteLine(unstructuredLog);
                throw new RpcException(new Status(StatusCode.FailedPrecondition, $"Can't access cart storage."));;
            }

            try
            {
                Hipstershop.Cart cart;
                var value = await _cache.GetAsync(userId);
                if (value == null)
                {
                    cart = new Hipstershop.Cart();
                    cart.UserId = userId;
                    cart.Items.Add(new Hipstershop.CartItem { ProductId = productId, Quantity = quantity });
                }
                else
                {
                    cart = Hipstershop.Cart.Parser.ParseFrom(value);
                    var existingItem = cart.Items.SingleOrDefault(i => i.ProductId == productId);
                    if (existingItem == null)
                    {
                        cart.Items.Add(new Hipstershop.CartItem { ProductId = productId, Quantity = quantity });
                    }
                    else
                    {
                        existingItem.Quantity += quantity;
                    }
                }
                await _cache.SetAsync(userId, cart.ToByteArray());
            }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.FailedPrecondition, $"[severity:error] - Can't access cart storage. {ex}"));
            }
        }

        public async Task EmptyCartAsync(string userId)
        {
            Console.WriteLine($"[severity:info] - EmptyCartAsync called with userId={userId}");

            try
            {
                var cart = new Hipstershop.Cart();
                await _cache.SetAsync(userId, cart.ToByteArray());
            }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.FailedPrecondition, $"[severity:error] - Can't access cart storage. {ex}"));
            }
        }

        public async Task<Hipstershop.Cart> GetCartAsync(string userId)
        {
            Console.WriteLine($"[severity:info] - GetCartAsync called with userId={userId}");

            try
            {
                // Access the cart from the cache
                var value = await _cache.GetAsync(userId);

                if (value != null)
                {
                    return Hipstershop.Cart.Parser.ParseFrom(value);
                }

                // We decided to return empty cart in cases when user wasn't in the cache before
                return new Hipstershop.Cart();
            }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.FailedPrecondition, $"[severity:error] - Can't access cart storage. {ex}"));
            }
        }

        public bool Ping()
        {
            try
            {
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
