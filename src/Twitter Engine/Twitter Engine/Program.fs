﻿open System.Collections.Generic

open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.Writers

open Suave.RequestErrors
open Suave.Logging

open Suave.Sockets
open Suave.Sockets.Control
open Suave.WebSocket

open Newtonsoft.Json


// Utility functions
let getString (rawForm: byte[]) =
    System.Text.Encoding.UTF8.GetString(rawForm)

let fromJson<'a> json =
    JsonConvert.DeserializeObject(json, typeof<'a>) :?> 'a


// Type definitions
type RegisterUserRequest = {
    Handle: string;
    FirstName: string;
    LastName: string;
    Password: string;
}

type RegisterUserResponse = {
    Success: bool;
}


// Models
type User = {
    Id: int;
    Handle: string;
    FirstName: string;
    LastName: string;
    Password: string;
    Followers: HashSet<int>;
    FollowingTo: HashSet<int>;
    Tweets: List<int>;
}

type Tweet = {
    Id: int;
    Content: string;
    PostedBy: int;
}


// Database
// User Id -> WebSocket instance mapping
let liveConnections = new Dictionary<int, WebSocket>()

// User Id -> User Instance mapping
let users = new Dictionary<int, User>()

// User Id -> UserStatus (online / offline) mapping
let userStatus = new Dictionary<int, bool>()

// User Handle -> User Id mapping
let handles = new Dictionary<string, int>()


// REST API functions
let registerUser = 
    request (fun r ->
        let req = r.rawForm |> getString |> fromJson<RegisterUserRequest>
        printfn "Register user request: %A" req
        let user: User = {
            Id = users.Count + 1;
            Handle = req.Handle;
            FirstName = req.FirstName;
            LastName = req.LastName;
            Password = req.Password;
            Followers = new HashSet<int>();
            FollowingTo = new HashSet<int>();
            Tweets = new List<int>();
        }
        
        users.Add((user.Id, user))
        users.[user.Id].FollowingTo.Add(user.Id) |> ignore
        users.[user.Id].Followers.Add(user.Id) |> ignore
        
        handles.Add((user.Handle, user.Id))
        
        userStatus.Add((user.Id, true))

        for u in users do
            printfn "%A" u

        let res: RegisterUserResponse = { Success = true; }
        res |> JsonConvert.SerializeObject |> CREATED
    )
    >=> setMimeType "application/json"


// Routes
let app : WebPart = 
    choose [
        POST >=> choose [
            path "/register" >=> registerUser
        ]

        NOT_FOUND "Resource not found. 404!" ]


[<EntryPoint>]
let main argv =
    startWebServer { defaultConfig with logger = Targets.create Verbose [||] } app
    0
