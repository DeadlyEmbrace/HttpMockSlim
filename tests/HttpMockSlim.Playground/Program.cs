﻿using System;
using System.Threading;
using HttpMockSlim.Model;

namespace HttpMockSlim.Playground
{
    class Program
    {
        static void Main()
        {
            HttpMock httpMock = new HttpMock();
            httpMock.Start("http://localhost:8000/");

            httpMock.Add("GET", "/test", (request, response) =>
            {
                Thread.Sleep(3000);
                response.SetBody($"{request}\r\nFoo!");
            });

            httpMock.Add("GET", "/", (request, response) => response.SetBody($"{request}\r\nWoah!"));

            httpMock.Add((request, response) => response.SetBody($"{request}\r\rMini Wild!"));

            httpMock.Add("GET", "/test", (request, response) => response.SetBody($"{request}\r\nBOOO!"));

            Console.WriteLine("Enter to exit...");
            Console.ReadLine();
        }
    }
}
