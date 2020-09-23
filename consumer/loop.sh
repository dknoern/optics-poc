#!/bin/bash

while true; do 
	DATE=`date`
	echo "hello, date is $DATE"

    echo "BROKER_URL=${BROKER_URL}"
    echo "QUEUE=${QUEUE}"
    /usr/bin/amqp-consume --url=$BROKER_URL -q $QUEUE -c 1
    sleep 3
done