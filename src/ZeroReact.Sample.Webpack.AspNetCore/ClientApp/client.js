import React from 'react';
import ReactDOM from 'react-dom';

window.React = React;
window.ReactDOM = ReactDOM;

// Expose components globally so ReactJS.NET can use them
var Components = require('expose?Components!./components');
