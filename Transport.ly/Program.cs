using Newtonsoft.Json;
using System;
using System.Collections.Generic;

using System.IO;
using System.Linq;


using System.Text.RegularExpressions;

namespace Transport.ly
{
    class Program
    {
        static List<Flight> _flightSchedule;
        static Dictionary<string, TripLocations> _rawOrderData;
        static List<Order> _scheduledOrders;
        static void Main(string[] args)
        {
            Console.WriteLine("Welcome!");
            DefaultMessages();
            string userResponse = "";
            do
            {
                userResponse = Console.ReadLine();

                if (userResponse.ToLower() == "load schedule")

                {

                    Console.WriteLine("Please enter flight info in the following format: <flight Number>,<departure city>,<arrival city>,<departure day>,<flight capacity>");
                    Console.WriteLine("To load default schedule, enter \"default\"");
                    Console.WriteLine("To display loaded schedule, please type \"display schedule\"");
                    Console.WriteLine("To clear loaded schedule, please type \"clear schedule\"");
                    Console.WriteLine("To stop loading the schedule, leave it empty and press enter.");
                    do
                    {
                        userResponse = Console.ReadLine();

                        if (userResponse.ToLower() == "default")
                        {
                            LoadFlightSchedule();
                            Console.WriteLine("Default schedule loaded.");
                        }
                        else if (userResponse.ToLower() == "display schedule")
                        {
                            DisplayFlightSchedule();
                        }
                        else if (userResponse.ToLower() == "clear schedule")
                        {
                            _flightSchedule = null;
                            Console.WriteLine("Flight schedule cleared.");
                        }
                        else if (userResponse.ToLower() != "")
                        {
                            var regex = new Regex(@"(\w+,\w+,\w+,\d,\d)");

                            if (regex.IsMatch(userResponse))
                            {
                                var flightArrary = userResponse.Split(',');

                                if (_flightSchedule == null) _flightSchedule = new List<Flight>();

                                _flightSchedule.Add(new Flight(flightArrary[0], flightArrary[1], flightArrary[2], Convert.ToInt32(flightArrary[3]), Convert.ToInt32(flightArrary[4])));

                                Console.WriteLine("Flight schedule updated.");
                            }
                            else
                            {
                                Console.WriteLine("Please enter the right format for flight info.");
                            }
                        }
                    }
                    while (userResponse.ToLower() != "");

                    DefaultMessages();

                }
                else if (userResponse.ToLower() == "display schedule")
                {
                    DisplayFlightSchedule();
                }
                else if (userResponse.ToLower() == "schedule orders")
                {
                    if (_flightSchedule?.Count() > 0)
                    {

                        Console.WriteLine("Please enter location of order files. Leave it empty to load default order file.");
                        bool exitLoop = false;
                        do
                        {
                            userResponse = Console.ReadLine();

                            exitLoop = LoadOrders(userResponse);

                        }
                        while (exitLoop == false);

                        ScheduleOrders();
                 
                    }
                    else
                    {
                        Console.WriteLine("No flight schedule loaded.");
                    }

                }
                else
                {
                    Console.WriteLine("Command not recognized.");
                }
            }

            while (userResponse.ToLower() != "exit");


        }

        private static void DefaultMessages()
        {
            Console.WriteLine("To load Schedule, please type \"load schedule\"");
            Console.WriteLine("To display loaded schedule, please type \"display schedule\"");
            Console.WriteLine("To load orders, please type \"schedule orders\"");
            Console.WriteLine("To quit, type \"exit\"");
        }

        private static void DisplayFlightSchedule()
        {
            if (_flightSchedule?.Count() > 0)
            {
                _flightSchedule.ForEach(flight =>
                {
                    Console.WriteLine("Flight: {0}, departure: {1}, arrival: {2}, day: {3}", flight.FlightNumber, flight.DepartureCity, flight.ArrivalCity, flight.DepartureDay);
                });
            }
            else
            {
                Console.WriteLine("No flight schedule loaded.");
            }
        }

        private static bool LoadOrders(string fileLocation)
        {
            if (string.IsNullOrEmpty(fileLocation))
            {
                var list = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames();

                using Stream stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(list[0]);
                using StreamReader streamReader = new StreamReader(stream);
                _rawOrderData = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, TripLocations>>(streamReader.ReadToEnd());
                return true;
            }
            else
            {
                try
                {
                    using StreamReader streamReader = new StreamReader(fileLocation);
                    _rawOrderData = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, TripLocations>>(streamReader.ReadToEnd());
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("There is an error loading the orders. Please re-enter order file location. Leave it empty to load default order file.");
                    return false;
                }

            }
        }
        private static void ScheduleOrders()
        {

            _scheduledOrders = new List<Order>();

          

            foreach (var rawOrder in _rawOrderData)
            {
                Order currentOrder = new Order(rawOrder.Key, rawOrder.Value);
                int flightCount = 0;
                do
                {
                    _flightSchedule[flightCount].LoadOrder(currentOrder);
                    flightCount++;

                } while (currentOrder.LoadedFlightInfo == null && flightCount < _flightSchedule.Count());

                _scheduledOrders.Add(currentOrder);
            }
            _scheduledOrders.ForEach(order =>
            {
                if (order.LoadedFlightInfo == null)
                {
                    Console.WriteLine("order: {0}, flighNumber: not scheduled", order.OrderNumber);
                }
                else
                {
                    Console.WriteLine("order: {0}, flighNumber: {1}, departure: {2}, Arrival: {3},  day: {4}", order.OrderNumber, order.LoadedFlightInfo.FlightNumber,
                        order.TripLocations.DepartureCity, order.TripLocations.ArrivalCity, order.LoadedFlightInfo.DepartureDay);
                }

            });
        }

        private static void LoadFlightSchedule()
        {
            _flightSchedule = new List<Flight>();

            _flightSchedule.Add(new Flight("1", "YUL", "YYZ", 1, 20));
            _flightSchedule.Add(new Flight("2", "YUL", "YYC", 1, 20));
            _flightSchedule.Add(new Flight("3", "YUL", "YVR", 1, 20));
            _flightSchedule.Add(new Flight("4", "YUL", "YYZ", 2, 20));
            _flightSchedule.Add(new Flight("5", "YUL", "YYC", 2, 20));
            _flightSchedule.Add(new Flight("6", "YUL", "YVR", 2, 20));
        }

        public class Flight
        {

            List<string> _loadedOrders;
            BasicFlightInfo _basicFlightInfo;
            TripLocations _tripLocations;
            int _flightCapacity;

            public Flight(string flightNumber, string departureCity, string arrivalCity, int departureDay, int flightCapacity)
            {

                _loadedOrders = new List<string>();
                _tripLocations = new TripLocations() { ArrivalCity = arrivalCity, DepartureCity = departureCity };
                _basicFlightInfo = new BasicFlightInfo() { DepartureDay = departureDay, FlightNumber = flightNumber };
                _flightCapacity = flightCapacity;


            }

            public string FlightNumber { get => _basicFlightInfo.FlightNumber; }
            public string DepartureCity { get => _tripLocations.DepartureCity; }

            public string ArrivalCity { get => _tripLocations.ArrivalCity; }
            public int DepartureDay { get => _basicFlightInfo.DepartureDay; }


            public void LoadOrder(Order order)
            {
                if (_loadedOrders.Count() < _flightCapacity)
                {
                    if (order.TripLocations.ArrivalCity == _tripLocations.ArrivalCity)
                    {
                        _loadedOrders.Add(order.OrderNumber);

                        order.ChangeBasicFlightInfo(_basicFlightInfo);
                        order.ChangeTripLocations(_tripLocations);
                    }
                }

            }


        }

        public class Order
        {

            BasicFlightInfo _loadedBasicFlightInfo;
            TripLocations _tripLocations;
            string _order;
            public string OrderNumber { get => _order; }
            public BasicFlightInfo LoadedFlightInfo { get => _loadedBasicFlightInfo; }

            public TripLocations TripLocations { get => _tripLocations; }

            public Order(string orderNumber, TripLocations tripLocations)
            {
                _order = orderNumber;
                _tripLocations = tripLocations;
            }
            public void ChangeBasicFlightInfo(BasicFlightInfo basicFlightInfo)
            {
                _loadedBasicFlightInfo = new BasicFlightInfo();
                _loadedBasicFlightInfo.DepartureDay = basicFlightInfo.DepartureDay;
                _loadedBasicFlightInfo.FlightNumber = basicFlightInfo.FlightNumber;
            }
            public void ChangeTripLocations(TripLocations tripLocations)
            {
                _tripLocations.DepartureCity = tripLocations.DepartureCity;
                _tripLocations.ArrivalCity = tripLocations.ArrivalCity;
            }
        }

        public class BasicFlightInfo
        {
            public string FlightNumber { get; set; }
            public int DepartureDay { get; set; }
        }

        public class TripLocations
        {

            public string DepartureCity { get; set; }
            [JsonProperty(PropertyName = "destination")]
            public string ArrivalCity { get; set; }
        }
    }
}
