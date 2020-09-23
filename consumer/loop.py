#!/usr/bin/env python

# Just prints standard out and sleeps for 10 seconds.
import sys
import time
i = 1
while True:
    print("waiting..." + str(i))
    i = i+1
    time.sleep(3)
