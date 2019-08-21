## Instructions installaction of testing environmet

Testing enviroment on Windows using Virutal box and Vagrant.
	1. Install Oracle Virtual Box
	2. Install Vagrant
	3. Init Vagrant in testing directory
		`vagrant init ubuntu/xenial64`
	4. Bring virtual box up
		`vagrant up`
	5. SSH to the environment
		`vagrant ssh`
	6. Create directory for redis
		`cd /home`
		`mkdir redis`
	7. Inside redis dirctory install redis
		`wget http://download.redis.io/redis-stable.tar.gz`
		`tar xvzf redis-stable.tar.gz`
		`cd redis-stable`
		`cd deps`
		`make hiredis jemalloc linenoise lua geohash-int`
		`cd ..`
		`make install`
	8. Add Redis configuration from this repo or your own
	9. Start Redis servers.