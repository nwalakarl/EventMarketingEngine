
namespace EventManagementSolution
{
    class MarketingEngine
    {

        static List<Event> events = new List<Event>{
                new Event(1, "Phantom of the Opera", "New York", new DateTime(2023,12,23)),
                new Event(2, "Metallica", "Los Angeles", new DateTime(2023,12,02)),
                new Event(3, "Metallica", "New York", new DateTime(2023,12,06)),
                new Event(4, "Metallica", "Boston", new DateTime(2023,10,23)),
                new Event(5, "LadyGaGa", "New York", new DateTime(2023,09,20)),
                new Event(6, "LadyGaGa", "Boston", new DateTime(2023,08,01)),
                new Event(7, "LadyGaGa", "Chicago", new DateTime(2023,07,04)),
                new Event(8, "LadyGaGa", "San Francisco", new DateTime(2023,07,07)),
                new Event(9, "LadyGaGa", "Washington", new DateTime(2023,05,22)),
                new Event(10, "Metallica", "Chicago", new DateTime(2023,01,01)),
                new Event(11, "Phantom of the Opera", "San Francisco", new DateTime(2023,07,04)),
                new Event(12, "Phantom of the Opera", "Chicago", new DateTime(2024,05,15))
            };

        static Customer customer = new Customer()
        {
            Id = 1,
            Name = "John",
            City = "New York",
            BirthDate = new DateTime(1995, 05, 10)
        };

        static void Main(string[] args)
        {
            

            Console.WriteLine("Welcome to EventEngine 1.0");

            int option = -1;

            while (option != 0)
            {
                // Display Event Management System action options.
                Console.WriteLine("Choose an option from the following list:");
                Console.WriteLine("\t1 - View all events");
                Console.WriteLine("\t2 - View all event locations at City");
                Console.WriteLine("\t3 - View all events close to customer birthday");
                Console.WriteLine("\t4 - Email events at Customer Location");
                Console.WriteLine("\t5 - Email 5 closest events at Customer Location");

                Console.WriteLine("\t0 - Quit");

                Int32.TryParse(Console.ReadLine(), out option);

                switch (option)
                {
                    case 1:
                        GetAllEvents();
                        break;
                    case 2:
                        GetAllEventsAtCity();
                        break;
                    case 3:
                        GetAllEventsCloseToCustomerBirthday();
                        break;
                    case 4:
                       EmailEventsAtCustomerCity();
                       break;
                    case 5:
                       Email5ClosestEventsAtCustomerCity();
                       break;
                }
            }

        }
                
        public static void SendCustomerNotifications(int index, Customer customer, Event e)
        {
            decimal price = GetPrice(e);
            int distance = GetDistance(e.City, customer.City);
            Console.WriteLine($"{index}. {customer.Name} from {customer.City} event {e.Name} " +
                $""+(distance > 0 ? $" ({distance} miles away)" : "") +
                $" at {e.Date} for ${price}");
        }

        public static void GetAllEvents()
        {
            int count = 0;
            foreach(Event e in events)
            {
                count++;
                SendCustomerNotifications(count, customer, e);
            }
        }

        public static void GetAllEventsCloseToCustomerBirthday()
        {
            //var eventsCloseToDate = events.Where(e => e.Date > DateTime.Now && e.Date.Month == customer.BirthDate.Month).ToList();//.Min(e => e.Date);
            //int count = 0;
            //foreach (Event e in eventsCloseToDate)
            //{
            //    count++;
            //    Console.WriteLine($"{count}. {e.Name} is taking place at {e.City} on the {e.Date}");
            //}

            var eventCloseToDate = events.Where(e => e.Date > DateTime.Now && e.Date.Month == customer.BirthDate.Month).ToList().Min(new EventDateComparer());
            
            if(eventCloseToDate !=null)
            {
                Console.WriteLine($"{eventCloseToDate.Name} is taking place at {eventCloseToDate.City} on the {eventCloseToDate.Date}");
            }
            else
            {
                Console.WriteLine($"No event close to {customer.Name}");
            }
            
        }

        public static void EmailEventsAtCustomerCity()
        {

            var eventsAtCustomerCity = events.Where(e => e.City.ToLower() == customer.City.ToLower()).ToList();

            eventsAtCustomerCity.Sort(new EventDistanceComparer(customer.City));

            int count = 0;
            foreach (Event e in eventsAtCustomerCity)
            {
                count++;
                SendCustomerNotifications(count, customer, e);
            }

        }

        public static void Email5ClosestEventsAtCustomerCity()
        {

            var eventsAtCustomerCity = GetNClosestEvents(customer.City);

            eventsAtCustomerCity.Sort(new EventDistanceComparer(customer.City));

            int count = 0;
            foreach (Event e in eventsAtCustomerCity)
            {
                count++;
                SendCustomerNotifications(count, customer, e);
            }

        }
       

        public static void GetAllEventsAtCity()
        {
            Console.WriteLine("Enter the name of City?");

            string city = Console.ReadLine();

            var eventsAtCity = GetEventsByCity(city);

            int count = 0;
            foreach (Event e in eventsAtCity)
            {
                count++;
                Console.WriteLine($"{count}. {e.Name} is taking place at {e.City} on the {e.Date}");
            }
        }

        public static List<Event> GetEventsByCity(string city)
        {

            var queryResult = from result in events
                              where result.City.ToLower() == city.ToLower()
                              select result;


            return queryResult.ToList();
        }

        public static int GetAlphabeticalDistance(string from, string to)
        {
            int result = 0;

            for(int i = 0; i < Math.Min(to.Length, from.Length); i++)
            {
                result += Math.Abs(from[i] - to[i]);
            }

            for(int i = 0; i < Math.Max(from.Length, to.Length); i++)
            {
                result += from.Length > to.Length ? from[i] : to[i];
            }

            return result;
        }

        /** Utility/API Services or functions **/
        private static Dictionary<string, int> CachedDistances = new Dictionary<string, int>();

        public static List<Event> GetNClosestEvents(string customerCity,int n = 5)
        {
            List<Event> result = new List<Event>();

            PriorityQueue<Event, int> priorityQueue = new PriorityQueue<Event, int>();

            foreach (Event e in events)
            {
                var distance = GetDistance(e.City, customerCity);

                priorityQueue.Enqueue(e, distance);
            }


            int count = 0;

            while (count < n)
            {
                result.Add(priorityQueue.Dequeue());

                count++;
            }

            result.Sort(new EventDistanceComparer(customerCity));

            return result;
        }

        public static int GetDistance(string fromCity, string toCity)
        {
            // Assuming Idempotent calls
            int numberOfTries = 5;
            bool isError = true;

            while(numberOfTries > 0 && isError == true)
            {
                numberOfTries--;

                try
                {
                    if (fromCity == null || toCity == null)
                    {
                        return 0;
                    }

                    if (fromCity.ToLower() == toCity.ToLower())
                    {
                        return 0;
                    }

                    // Computes a bidirectional key for the cache.
                    string[] citiesArray = { fromCity, toCity };

                    Array.Sort(citiesArray, (x, y) => x.CompareTo(y));

                    string distanceCacheKey = String.Join('-', citiesArray);

                    if (CachedDistances.ContainsKey(distanceCacheKey))
                    {
                        return CachedDistances[distanceCacheKey];
                    }
                    else
                    {
                        int computedDistance = GetAlphabeticalDistance(fromCity, toCity);
                        CachedDistances.Add(distanceCacheKey, computedDistance);

                        return computedDistance;
                    }

                    isError = false;
                    

                }
                catch (Exception)
                {
                    // Returns a zero for the distance
                   
                    //isError = true;
                }
                
            }

            return 0;

        }


        public static int GetPrice(Event e)
        {
            return (GetAlphabeticalDistance(e.City, "") + GetAlphabeticalDistance(e.Name, "")) / 10;
        }

        /** Comparators **/
        public class EventPriceComparer : IComparer<Event>
        {
            public int Compare(Event? x, Event? y)
            {
                if (x == null || y == null)
                {
                    return 0;
                }

                return GetPrice(x) - GetPrice(y);
            }
        }

        public class EventDistanceComparer : IComparer<Event>
        {
            private string _relativeEventCity;
            public EventDistanceComparer(string relativeEventCity)
            {
                _relativeEventCity = relativeEventCity;
            }


            public int Compare(Event? x, Event? y)
            {
                if (x == null || y == null)
                {
                    return 0;
                }

                int eventXDistance = 0;
                int eventYDistance = 0;

                if (_relativeEventCity != null)
                {
                    eventXDistance = GetDistance(_relativeEventCity, x.City);
                    eventYDistance = GetDistance(_relativeEventCity, y.City);
                }

                if (eventXDistance == eventYDistance)
                {
                    int eventXPrice = GetPrice(x);
                    int eventYPrice = GetPrice(y);

                    return eventXPrice - eventYPrice;
                }


                return eventXDistance - eventYDistance;
            }
        }
    }

    public class Event
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string City { get; set; }
        public DateTime Date { get; set; }

        public Event(int id, string name, string city, DateTime date)
        {
            this.Id = id;
            this.Name = name;
            this.City = city;
            this.Date = date;
        }
    }

    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string City { get; set; }
        public DateTime BirthDate { get; set; }
    }

    public class EventDateComparer : IComparer<Event>
    {
        public int Compare(Event? x, Event? y)
        {
            return x.Date.CompareTo(y.Date);
        }
    }


}
