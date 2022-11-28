// Dictionaries for player & rolling stock data
var _playerData = {}; // Time: [X, Y, Z, ROT]
var _carData = {};    // CID_TYP: {Time: [X, Y, Z, ROT, SPD, H, CgTYP, CgCAT, CgH]}
const _idSep = ' ';   //    ^ This symbol here
const _activeCars = new Set();

const _worldDimensions = [16360, 16360];

var _traces = [];
var _frames = [];
var _frame = 0;
var _numFrames = 0;
var _fps = 24;
var _animating = false;
var _verboseTrace = false;
var _prevTime = 0;

// Each major version indicates a change/addition to the columns of data created
var _tracerVersion;

const _playerDataIndices = [
    'X',
    'Y',
    'Z',
    'ROT'
];

const _allCarDataIndices = [
    //'CID',
    //'TYP',
    'X',    // ----------
    'Y',    // - v0+
    'Z',    // -
    'ROT',  // -
    'SPD',  // ----------
    'H',    // ----------
    'CgTYP',// - v1+
    'CgCAT',// -
    'CgH'   // ----------
];

var _carDataIndices = _allCarDataIndices;

const _colours = {
    'Player':   '#1f77b4',
    'Caboose':  '#d62728',
    'HandCar':  '#d62728',
    'DE2':      '#ff6600',
    'DE6':      '#7e5c3a',
    'SH282':    '#111111',
    'Tender':   '#111111',
    'Mod':      '#c5558e',

    'MIL1':     '#79c633',
    'MIL2':     '#68aa2c',
    'MIL3':     '#568d42',
    // CXN - nuclear flask
    // CXB - mil. boxcar
    // CXF - mil. flatcar

    // 'HZMT1':    '#ffa80f',
    // 'HZMT2':    '#fe8116',
    // 'HZMT3':    '#fe5a1d',
    // 'Inert':    '#696969',

    'CFF':      '#bdbdbd', // flatbed
    'CFFL':     '#8b8b8b',
    'CFS':      '#bdbdbd', // flatbed with stakes
    'CFSL':     '#696969',
    'CVT':      '#eeb609', // car car
    'CVTL':     '#c69320', // car car with cars
    'CBK':      '#fcb033', // gondola/hopper
    'CBKL':     '#fa8607',
    'CBX':      '#b5651d', // boxcar
    'CBXL':     '#964b00',
    'CRF':      '#b5651d', // refrigerated boxcar
    'CRFL':     '#673400',
    'CGS':      '#e6c16a', // gas tanker (orange/blue)
    'CGSL':     '#ce9f56',
    'CCH':      '#c34632', // chemical tanker (black)
    'CCHL':     '#9e1711',
    'COL':      '#555555', // oil tanker (white/chrome)
    'COLL':     '#333333',
    'CPS':      '#a3a3ff', // Passenger car
    'CPSL':     '#7879ff',

    'Unknown':  '#9969c7', // (usually) A modded car that was assigned the generic C prefix, rather than one of the triples above
    'UnknownL': '#6a359c'
};

$(document).ready(function() {
    // Allow file to be drag&dropped to load
    let dragNDrop = $('#pathAnimator');
    dragNDrop.on('dragover', function(e) {
        e.preventDefault();
        e.stopPropagation();
    });
    dragNDrop.on('dragenter', function(e) {
        e.preventDefault();
        e.stopPropagation();
    });
    dragNDrop.on('drop', function(e) {
        e.preventDefault();
        e.stopPropagation();
        $('#filePicker').val('');
        loadFile(e.originalEvent.dataTransfer.files[0]);
    });

    // Allow user-selected file
    $('#filePicker').change(function() {
        if (this.files && this.files[0]) {
            loadFile(this.files[0]);
        }
    });

    // Colour the Key/Legend
    let colourKeys = Object.keys(_colours);
    for (let i=0; i < colourKeys.length; i++) {
        let spot = colourKeys[i];
        $('#' + spot).css("background-color", _colours[spot]);
    }

    // Create FPS slider
    let fpsSlider = document.getElementById('fpsSlider');
    noUiSlider.create(fpsSlider, {
        start: [_fps],
        tooltips: [wNumb({decimals: 0})],
        step: 1,
        range: {
            'min': [1],
            'max': [60]
        },
        format: wNumb({
            decimals: 0
        })
    });
    fpsSlider.noUiSlider.on('change', function() {
        _fps = fpsSlider.noUiSlider.get();
    });

    $('#plotButton').click(plotData);
    $('#timelapseButton').click(animateData);
});

////////////////////////////////////////////////////////////////////////////////
// Functions for loading & interpreting the CSV file //
////////////////////////////////////////////////////////////////////////////////

/**
 * Reads & interprets the given file
 */
function loadFile(file) {
    if (file.type != 'text/csv') {
        if (file.type.match(/image.*/)) {
            importBackgroundImage(file);
        } else {
            console.warn(`File must be csv, got ${file.type}`)
            alert('File must be .csv!');
        }
        return;
    }

    console.log(`Reading file ${file.name}`)

    let reader = new FileReader();
    reader.onload = function() {
        interpretFile(this.result);
    }
    reader.readAsText(file);
}

/**
 * Interprets the contents of a file
 */
function interpretFile(fileString) {
    console.log('Interpreting file...')
    _playerData = {};
    _carData = {};
    _activeCars.clear();
    let lines = fileString.split('\n');
    console.log(`Reading ${lines.length} lines...`);
    interpretMetadataLine(lines[0].trim().split(','));
    for (let i=1; i < lines.length; i++) {
        let elements = lines[i].trim().split(',');

        let time = parseInt(elements.splice(0, 1));
        
        // Deal with the player data
        interpretPlayerLine(time, elements.splice(0, _playerDataIndices.length));

        // Deal with the car(s) data
        while (elements.length > 0) {
            interpretCarLine(time, elements.splice(0, _carDataIndices.length + 2));
        }

        // Deal with the remaining spawned cars
        if (!_verboseTrace) {
            for (const car of _activeCars) {
                if (!_carData[car][time]) {
                    _carData[car][time] = _carData[car][_prevTime]
                }
              }
        }

        _prevTime = time;
    }
    _traces = getPlottableTraces();
    _frames = getPlottableFrames();
    console.log('Done');
    plotData();
}

/**
 * Interprets an array of data about the tracer that created the file
 */
function interpretMetadataLine(line) {
    if (line[0] != 'DVPathTracer') {
        // No metadata line
        console.log("Tracer version: 0.3.0 or earlier");
        _carDataIndices = _allCarDataIndices.slice(0,5);
        _tracerVersion = 0;
        _verboseTrace = true;
    } else {
        console.log(`Tracer version: ${line[1]}`);
        _carDataIndices = _allCarDataIndices;
        _tracerVersion = 1;

        _verboseTrace = (line[3] == 'True');
        console.log(`Verbose trace: ${_verboseTrace}`);
    }
}

/**
 * Interprets an array of data relevant to info about the player
 */
function interpretPlayerLine(time, line) {
    if (line.length < 4 || line[0] == '' || line[0] == 'PPosX') {
        return
    }
    _playerData[time] = [
        parseFloat(line[0]),
        parseFloat(line[1]),
        parseFloat(line[2]),
        parseFloat(line[3])
    ];
}

/**
 * Interprets an array of data relevant to info about one piece of rolling stock
 */
function interpretCarLine(time, line) {
    if (line.length < 6 || line[0] == '' ||  line[0] == 'CID') {
        return;
    }
    let idType = line[0] + _idSep + line[1];
    if (!_verboseTrace && line[2] == 'Removed') {
        _activeCars.delete(idType);
        return
    }
    let result = [
        parseFloat(line[2]),
        parseFloat(line[3]),
        parseFloat(line[4]),
        parseFloat(line[5]),
        parseFloat(line[6])
    ];
    if (_tracerVersion >= 1) {
        result.push(
            parseFloat(line[7]),
            line[8],
            line[9],
            parseFloat(line[10])
        );
    }
    if (!_carData[idType]) {
        _carData[idType] = {};
    }
    _carData[idType][time] = result;
    _activeCars.add(idType);
}

////////////////////////////////////////////////////////////////////////////////
// End // Functions for loading & interpreting the CSV file //
////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////
// Functions for graphing & animating the data //
////////////////////////////////////////////////////////////////////////////////

/**
 * Returns a list of all plottable traces, grouped by object
 */
function getPlottableTraces() {
    let dataPlot = [];
    let orderedPlayerKeys = Object.keys(_playerData).sort((a, b) => a - b);
    dataPlot.push({
        x: [], y: [],
        name: 'Player',
        type: 'scatter',
        mode: 'lines',
        line: {
            width: 3,
            color: _colours['Player']
        }
    });
    for (let i=0; i < orderedPlayerKeys.length; i++) {
        let key = orderedPlayerKeys[i];
        last(dataPlot).x.push(_playerData[key][_playerDataIndices.indexOf('X')]);
        last(dataPlot).y.push(_playerData[key][_playerDataIndices.indexOf('Z')]);
    }

    for (let carID in _carData) {
        // Don't include freight/passenger cars in the static trace
        if (!carID.startsWith('L') && !carID.includes('Caboose')) {
            continue;
        }
        let car = _carData[carID];
        let orderedCarKeys = Object.keys(car).sort((a, b) => a - b);
        let carType = carID.split(_idSep)[1];
        let spotColour = _colours[carType] ? _colours[carType] : _colours['Mod'];
        dataPlot.push({
            x: [], y: [],
            name: carID,
            type: 'scatter',
            mode: 'lines+markers',
            line: {
                width: 3,
                color: spotColour
            },
            marker: {
                size: 3,
                color: spotColour
            }
        });
        for (let i=0; i < orderedCarKeys.length; i++) {
            let key = orderedCarKeys[i];
            last(dataPlot).x.push(car[key][_carDataIndices.indexOf('X')]);
            last(dataPlot).y.push(car[key][_carDataIndices.indexOf('Z')]);
        }
    }

    return dataPlot;
}

/**
 * Plot all traces on a graph
 */
function plotData() {
    _animating = false;
    $('#plotButton').attr('disabled', true);
    $('#timelapseButton').attr('disabled', false);
    $('#animationPlot').empty()
    $('#fpsOption').addClass('d-none');

    console.log('Plotting data');

    let layout = {
        showlegend: true,
        xaxis: {
            range: [0, _worldDimensions[0]],
            visible: false,
            showgrid: false
        },
        yaxis: {
            range: [0, _worldDimensions[1]],
            scaleanchor: 'x',
            visible: false,
            showgrid: false
        },
        dragmode: 'pan',
    };

    Plotly.newPlot( 'animationPlot', _traces, layout, { scrollZoom: true } );
    
    if ($('#animationPlot').attr('src')) {
        console.log("Adding existing background image");
        addBackgroundImage();
    }
}

/**
 * Returns a list of all plottable objects, grouped by frame in time
 */
function getPlottableFrames() {
    let dataFrames = [];
    _numFrames = 0;
    let orderedPlayerKeys = Object.keys(_playerData).sort((a, b) => a - b);
    for (i=0; i < orderedPlayerKeys.length; i++) {
        let recordTime = orderedPlayerKeys[i];
        // Each frame is a scatter plot of every player & piece of rolling stock
        let dataPlot = {
            x: [_playerData[recordTime][_playerDataIndices.indexOf('X')]],
            y: [_playerData[recordTime][_playerDataIndices.indexOf('Z')]],
            color: [_colours['Player']]
        };

        for (let carID in _carData) {
            let car = _carData[carID];
            // If there is a record for this car at this time:
            if (car[recordTime]) {
                dataPlot.x.push(car[recordTime][_carDataIndices.indexOf('X')]);
                dataPlot.y.push(car[recordTime][_carDataIndices.indexOf('Z')]);
                let carType = carID.split(_idSep)[1];
                let spotColour = _colours[carType] ? _colours[carType] : _colours['Mod'];
                if (_tracerVersion >= 1 && !carID.startsWith('L') && !carID.includes('Caboose') && !carID.includes('Tender') && !carID.includes('HandCar')) {
                    // Pick out military cargo, then sort the rest into groups
                    let cargoCategory = car[recordTime][_carDataIndices.indexOf('CgCAT')];
                    if (['MIL1', 'MIL2', 'MIL3'].includes(cargoCategory)) {
                        spotColour = _colours[cargoCategory];
                    } else {
                        // Group by car type
                        let cgSuffix = car[recordTime][_carDataIndices.indexOf('CgTYP')] == 'None' ? '' : 'L';
                        let subID = carID.substring(0, 3);
                        spotColour = _colours[subID] ? _colours[subID + cgSuffix] : _colours['Unknown' + cgSuffix];
                    }
                }
                dataPlot.color.push(spotColour);
            }
        }
        dataFrames.push(dataPlot);
    }
    _numFrames = orderedPlayerKeys.length;
    
    return dataFrames;
}

/**
 * Update each point being plotted in the timelapse
 */
function update() {
    let nextFrame = 1000/_fps;
    if (!_animating) {
        return;
    }
    let data = {
        data: [{
            x: _frames[_frame].x,
            y: _frames[_frame].y,
            marker:{
                size: 10,
                color: _frames[_frame].color
            },
        }]
    };
    let format = {
        transition: {
          duration: 0,//1000/_fps,
          //easing: 'linear',
        },
        frame: {
            duration: nextFrame,
            redraw: false
        }
    };
    Plotly.animate( 'animationPlot', data, format);
    _frame = _frame >= _numFrames - 1 ? 0 : _frame + 1;
    setTimeout(requestAnimationFrame, nextFrame, update);
}

/**
 * Begin animating a timelapse of the objects positions
 */
function animateData() {
    _animating = false;
    $('#timelapseButton').attr('disabled', true);
    $('#plotButton').attr('disabled', false);
    $('#animationPlot').empty()
    $('#fpsOption').removeClass('d-none');
    _frame = 0;

    console.log('Animating data');

    Plotly.newPlot('animationPlot', [{
        x: _frames[0].x,
        y: _frames[0].y,
        mode: 'markers'
    }], {
        xaxis: {
            range: [0, _worldDimensions[0]],
            visible: false,
            showgrid: false
        },
        yaxis: {
            range: [0, _worldDimensions[1]],
            scaleanchor: 'x',
            visible: false,
            showgrid: false
        },
        dragmode: 'select',
    }, {
        scrollZoom: true,
        modeBarButtonsToRemove: ['zoom2d', 'pan2d'] // TODO: Figure out why these cause errors
    });
    
    if ($('#animationPlot').attr('src')) {
        console.log("Adding existing background image");
        addBackgroundImage();
    }

    _animating = true;
    requestAnimationFrame(update);
}

/**
 * Add the set image in the background of the existing plot
 */
function addBackgroundImage() {
    if (!$('#animationPlot').attr('src')) {
        console.warn("No image set");
        return
    }
    let update = {
        images: [
            {
                'source': $('#animationPlot').attr('src'),
                'xref': 'x',
                'yref': 'y',
                'x': 0,
                'y': _worldDimensions[1],
                'sizex': _worldDimensions[0],
                'sizey': _worldDimensions[1],
                'sizing': 'stretch',
                'opacity': 0.6,
                'layer': 'below'
            }
        ]
    };
    Plotly.relayout('animationPlot', update);
}

/**
 * Instead of just putting a local image in the background we have to get the
 * user to import it. Thanks cors
 */
function importBackgroundImage(image) {
    console.log(`Setting ${image.name} as background image`);
    var FR = new FileReader();
    FR.addEventListener('load', function(e) {
        $('#animationPlot').attr('src', e.target.result);
        if ($('#animationPlot').children().length) {
            addBackgroundImage();
        } else {
            alert("Image will render when your traced path (csv) is loaded.");
        }
    });
    FR.readAsDataURL(image);
}

////////////////////////////////////////////////////////////////////////////////
// End // Functions for graphing & animating the data //
////////////////////////////////////////////////////////////////////////////////

/**
 * Return the last item in the given array
 */
function last(array) {
    return array[array.length - 1];
}
