# AppKeeper

### Tiny utillity-service to keep an application running.
Intended to be installed as a service on target-host.

Will look for current processes, and if target-application is not found, will attempt to restart it, while impersonating current user. <br>
The usecase is only intended for applications that interact directly with a user-session, ie. has a GUI.