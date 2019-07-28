Get started with Redis

Testing enviroment on Windows using virutal box and vagrant.
	1. Install Oracle virtual box
	2. Install Vagrant
	3. Init vagrant in testing directory
		vagrant init ubuntu/xenial64
	4. Bring virtual box up
		vagrant up
	5. SSH to the environment
		vagrant ssh
	6. Create directory for redis
		cd /home
		mkdir redis
	7. Inside redis dirctory install redis
		wget http://download.redis.io/redis-stable.tar.gz
		tar xvzf redis-stable.tar.gz
		cd redis-stable
			cd deps
			make hiredis jemalloc linenoise lua geohash-int
		cd ..
		make install


All things that I had to install in Ubutnu 16.04:
apt-get update
apt install make
apt install gcc
apt-get install -y tcl

Usefull links:

https://medium.com/@dhammikasamankumara/getting-started-with-redis-cluster-on-windows-6435d0ffd87
https://redis.io/topics/quickstart