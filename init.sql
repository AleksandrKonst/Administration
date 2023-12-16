CREATE TABLE weather (
    id SERIAL NOT NULL,
    name character varying NOT NULL
);

INSERT INTO weather (id, name) VALUES (1, 'Пасмурно'), (2, 'Облачно');